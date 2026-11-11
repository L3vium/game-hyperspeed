using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Com.IsartDigital.Hyperspeed
{
    // Field of view past what a perspective camera can do (2026-11-11 : "the FOV must be huge, more than the maximum").
    // Six cameras (front, right, left, up, down, back), children of the main camera, render the world into textures ;
    // the main camera then draws only one full screen triangle that recomposes them through a wide angle projection
    // (Hyperspeed/Panorama). URP post process, the Stylize pass and the UI all apply on the recomposed image as before.
    // Only the faces the current projection needs are rendered. CameraRig drives the FOV and the projection shape.
    [DefaultExecutionOrder(1000)]
    public class PanoramaRig : MonoBehaviour
    {
        public const int LAYER = 30;
        // tan of half a face's FOV : a little more than 90 degrees so the seams never sample past the edge.
        private const float GUARD = 1.04f;
        private const int FACES = 6;

        private static readonly int PROJ = Shader.PropertyToID("_HS_Proj");
        private static readonly int PANO_FACES = Shader.PropertyToID("_HS_PanoFaces");
        private static readonly int EDGE_COLOR = Shader.PropertyToID("_HS_PanoEdgeColor");
        private static readonly int[] FACE_TEX = { Shader.PropertyToID("_Face0"), Shader.PropertyToID("_Face1"), Shader.PropertyToID("_Face2"), Shader.PropertyToID("_Face3"), Shader.PropertyToID("_Face4"), Shader.PropertyToID("_Face5") };
        private static readonly int[] FACE_ST = { Shader.PropertyToID("_HS_FaceST0"), Shader.PropertyToID("_HS_FaceST1"), Shader.PropertyToID("_HS_FaceST2"), Shader.PropertyToID("_HS_FaceST3"), Shader.PropertyToID("_HS_FaceST4"), Shader.PropertyToID("_HS_FaceST5") };
        private static readonly Quaternion[] ROTATION =
        {
            Quaternion.identity, Quaternion.Euler(0f, 90f, 0f), Quaternion.Euler(0f, -90f, 0f),
            Quaternion.Euler(-90f, 0f, 0f), Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f)
        };
        private static readonly string[] NAMES = { "PanoFace_Front", "PanoFace_Right", "PanoFace_Left", "PanoFace_Up", "PanoFace_Down", "PanoFace_Back" };
        // Resolution of each face relative to the front one.
        private static readonly float[] RES_SCALE = { 1f, 0.7f, 0.7f, 0.5f, 0.55f, 0.4f };

        public static PanoramaRig Active { get; private set; }

        // Horizontal FOV in degrees, shape (0.5 stereographic .. 0 equidistant), cylinder blend (0 fisheye, 1 Panini).
        public float hfov = 200f;
        public float shape = 0.5f;
        public float cylinder = 0.6f;

        public bool On => _on;
        public Camera Main => _main;
        public float Edge { get; private set; } = 1f;

        private Camera _main;
        private readonly Camera[] _faces = new Camera[FACES];
        private readonly RenderTexture[] _targets = new RenderTexture[FACES];
        private readonly bool[] _needed = new bool[FACES];
        private Material _material;
        private GameObject _screen;
        private bool _on;
        private int _mainMask;
        private CameraClearFlags _mainClear;
        private int _resolution;

        public static PanoramaRig Attach(Camera pMain)
        {
            PanoramaRig lRig = pMain.gameObject.AddComponent<PanoramaRig>();
            lRig._main = pMain;
            lRig._mainMask = pMain.cullingMask & ~(1 << LAYER);
            lRig._mainClear = pMain.clearFlags;
            Active = lRig;
            lRig.SetEnabled(GameSession.WideAngle);
            return lRig;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
            for (int i = 0; i < FACES; i++) if (_targets[i] != null) { _targets[i].Release(); Destroy(_targets[i]); }
        }

        public void SetEnabled(bool pOn)
        {
            if (pOn && _material == null) Build();
            _on = pOn && _material != null;
            for (int i = 0; i < FACES; i++) if (_faces[i] != null) _faces[i].enabled = _on;
            if (_screen != null) _screen.SetActive(_on);
            UniversalAdditionalCameraData lData = _main.GetUniversalAdditionalCameraData();
            if (_on)
            {
                // The main camera draws nothing of the world any more : only the recomposed image, then the post.
                _main.cullingMask = 1 << LAYER;
                _main.clearFlags = CameraClearFlags.SolidColor;
                _main.backgroundColor = Color.black;
                lData.renderShadows = false;
                lData.requiresDepthOption = CameraOverrideOption.Off;
            }
            else
            {
                _main.cullingMask = _mainMask;
                _main.clearFlags = _mainClear;
                lData.renderShadows = true;
                lData.requiresDepthOption = CameraOverrideOption.On;
            }
        }

        private void Build()
        {
            Shader lShader = Shader.Find("Hyperspeed/Panorama");
            if (lShader == null)
            {
                Debug.LogError("[HS-PANO] Hyperspeed/Panorama shader missing");
                return;
            }
            _material = new Material(lShader) { name = "Panorama" };
            _resolution = ResolutionFor(GameSession.PanoramaQuality);
            UniversalAdditionalCameraData lMainData = _main.GetUniversalAdditionalCameraData();
            for (int i = 0; i < FACES; i++)
            {
                GameObject lObject = new GameObject(NAMES[i]);
                lObject.transform.SetParent(_main.transform, false);
                lObject.transform.localRotation = ROTATION[i];
                Camera lFace = lObject.AddComponent<Camera>();
                // CopyFrom also copies the transform : the face's own orientation is set after it.
                lFace.CopyFrom(_main);
                lObject.transform.SetParent(_main.transform, false);
                lObject.transform.localPosition = Vector3.zero;
                lObject.transform.localRotation = ROTATION[i];
                lFace.cullingMask = _mainMask;
                lFace.clearFlags = _mainClear;
                lFace.fieldOfView = 2f * Mathf.Atan(GUARD) * Mathf.Rad2Deg;
                lFace.aspect = 1f;
                lFace.depth = _main.depth - 1f;
                lFace.allowMSAA = false;
                lFace.allowHDR = true;

                int lSize = Mathf.Max(64, Mathf.RoundToInt(_resolution * RES_SCALE[i] / 32f) * 32);
                RenderTexture lTarget = new RenderTexture(lSize, lSize, 24, RenderTextureFormat.DefaultHDR) { name = NAMES[i], wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, antiAliasing = 1 };
                lTarget.Create();
                lFace.targetTexture = lTarget;
                _targets[i] = lTarget;
                _material.SetTexture(FACE_TEX[i], lTarget);
                _material.SetVector(FACE_ST[i], new Vector4(1f, 1f, 0f, 0f));
                UniversalAdditionalCameraData lData = lFace.GetUniversalAdditionalCameraData();
                // Renderer 1 : no Stylize, no SSAO (they run once, on the recomposed image).
                lData.SetRenderer(1);
                lData.renderPostProcessing = false;
                lData.antialiasing = AntialiasingMode.None;
                // Only the front face pays for shadows and the scene colour copy (impact frames sample it).
                lData.requiresColorOption = i == 0 ? CameraOverrideOption.On : CameraOverrideOption.Off;
                lData.requiresDepthOption = CameraOverrideOption.Off;
                lData.renderShadows = i == 0;
                // Side faces : small debris and props far away are not worth drawing there.
                if (i != 0)
                {
                    float[] lCull = new float[32];
                    lCull[Layers.DEBRIS] = 700f;
                    lCull[Layers.PROP] = 2200f;
                    lFace.layerCullDistances = lCull;
                }
                lData.volumeLayerMask = 0;
                lData.stopNaN = false;
                lData.dithering = false;
                _faces[i] = lFace;
            }

            // One triangle covering the screen, on a layer only the main camera sees, never culled.
            _screen = new GameObject("PanoramaScreen");
            _screen.layer = LAYER;
            _screen.transform.SetParent(_main.transform, false);
            Mesh lMesh = new Mesh { name = "PanoramaTriangle" };
            lMesh.vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(-1f, 3f, 0f), new Vector3(3f, -1f, 0f) };
            lMesh.triangles = new[] { 0, 1, 2 };
            lMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);
            _screen.AddComponent<MeshFilter>().sharedMesh = lMesh;
            MeshRenderer lRenderer = _screen.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = _material;
            lRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lRenderer.receiveShadows = false;
            _material.SetVector(PANO_FACES, new Vector4(GUARD, 1f, 0f, 0f));
        }

        private static int ResolutionFor(int pQuality) => pQuality <= 0 ? 640 : pQuality == 1 ? 960 : 1408;

        private void LateUpdate()
        {
            if (!_on || _material == null) return;
            float lAspect = Mathf.Max(0.5f, _main.aspect);
            float lHalf = Mathf.Clamp(hfov, 60f, 330f) * 0.5f * Mathf.Deg2Rad;
            float lShape = ProjectionMath.SafeShape(shape, lHalf);
            Edge = ProjectionMath.F(lHalf, lShape);
            _material.SetVector(PROJ, new Vector4(Edge, lShape, Mathf.Clamp01(cylinder), lAspect));
            // Set every frame : values the shader does not declare can be dropped when shaders reload.
            _material.SetVector(PANO_FACES, new Vector4(GUARD, 1f, 0f, 0f));
            for (int i = 0; i < FACES; i++)
            {
                _material.SetVector(FACE_ST[i], new Vector4(1f, 1f, 0f, 0f));
                if (_targets[i] != null) _material.SetTexture(FACE_TEX[i], _targets[i]);
            }
            _material.SetColor(EDGE_COLOR, RenderSettings.fogColor);
            CullFaces(Edge, lShape, lAspect);
        }

        // Only the faces some pixel of the screen looks into are rendered.
        private void CullFaces(float pEdge, float pShape, float pAspect)
        {
            for (int i = 0; i < FACES; i++) _needed[i] = false;
            _needed[0] = true;
            const int GRID_X = 24;
            const int GRID_Y = 14;
            for (int y = 0; y <= GRID_Y; y++)
            {
                for (int x = 0; x <= GRID_X; x++)
                {
                    Vector2 lNdc = new Vector2(x / (float)GRID_X * 2.06f - 1.03f, y / (float)GRID_Y * 2.06f - 1.03f);
                    Vector3 lD = ProjectionMath.ScreenToDirection(lNdc, pEdge, pShape, cylinder, pAspect);
                    _needed[FaceOf(lD)] = true;
                }
            }
            for (int i = 0; i < FACES; i++) if (_faces[i] != null && _faces[i].enabled != _needed[i]) _faces[i].enabled = _needed[i];
        }

        private static int FaceOf(Vector3 pD)
        {
            float lX = Mathf.Abs(pD.x), lY = Mathf.Abs(pD.y), lZ = Mathf.Abs(pD.z);
            if (lZ >= lX && lZ >= lY) return pD.z > 0f ? 0 : 5;
            if (lX >= lY) return pD.x > 0f ? 1 : 2;
            return pD.y > 0f ? 3 : 4;
        }

        // ---------------------------------------------------------------- screen helpers (HUD markers)

        // World point to screen pixels through the wide projection. z < 0 : behind or outside the image.
        public bool WorldToScreen(Vector3 pWorld, out Vector3 pScreen)
        {
            Vector3 lLocal = _main.transform.InverseTransformPoint(pWorld);
            float lAspect = Mathf.Max(0.5f, _main.aspect);
            float lShape = ProjectionMath.SafeShape(shape, Mathf.Clamp(hfov, 60f, 330f) * 0.5f * Mathf.Deg2Rad);
            Vector2 lNdc;
            bool lInside = ProjectionMath.DirectionToScreen(lLocal, Edge, lShape, cylinder, lAspect, out lNdc);
            pScreen = new Vector3((lNdc.x * 0.5f + 0.5f) * Screen.width, (lNdc.y * 0.5f + 0.5f) * Screen.height, lInside ? lLocal.magnitude : -1f);
            return lInside;
        }
    }
}
