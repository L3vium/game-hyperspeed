using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Com.IsartDigital.Hyperspeed
{
    // Canvas drawn through its own perspective camera, stacked over the game camera : whatever rotates or moves in depth
    // on it gets real perspective. Titles, the countdown and the race gauges live here.
    public static class UIPerspective
    {
        public const int LAYER = 5;
        private const float FOV = 34f;
        private const float PLANE = 120f;

        public static Canvas Create(string pName, int pOrder, Transform pParent, Camera pGameCamera)
        {
            GameObject lCameraObject = new GameObject(pName + "Camera");
            lCameraObject.transform.SetParent(pParent, false);
            lCameraObject.transform.position = new Vector3(0f, -30000f, 0f);
            Camera lCamera = lCameraObject.AddComponent<Camera>();
            lCamera.fieldOfView = FOV;
            lCamera.nearClipPlane = 1f;
            lCamera.farClipPlane = PLANE * 4f;
            lCamera.cullingMask = 1 << LAYER;
            lCamera.clearFlags = CameraClearFlags.Nothing;
            UniversalAdditionalCameraData lData = lCamera.GetUniversalAdditionalCameraData();
            lData.renderType = CameraRenderType.Overlay;
            lData.SetRenderer(1);
            lData.renderPostProcessing = false;
            lData.renderShadows = false;
            UniversalAdditionalCameraData lBase = pGameCamera.GetUniversalAdditionalCameraData();
            lBase.cameraStack.Add(lCamera);

            Canvas lCanvas = UIFactory.CreateCanvas(pName, pOrder, pParent);
            lCanvas.gameObject.layer = LAYER;
            lCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            lCanvas.worldCamera = lCamera;
            lCanvas.planeDistance = PLANE;
            lCanvas.gameObject.AddComponent<UIStackLink>().Init(lCamera, lBase);
            return lCanvas;
        }
    }

    // Destroying a perspective canvas also takes its camera out of the stack.
    public class UIStackLink : MonoBehaviour
    {
        private Camera _camera;
        private UniversalAdditionalCameraData _base;

        public void Init(Camera pCamera, UniversalAdditionalCameraData pBase)
        {
            _camera = pCamera;
            _base = pBase;
        }

        private void OnDestroy()
        {
            if (_base != null && _camera != null) _base.cameraStack.Remove(_camera);
            if (_camera != null) Destroy(_camera.gameObject);
        }
    }

    // Big word that really has a volume : stacked copies going back in depth, leaning with the mouse.
    public class UI3DText : MonoBehaviour
    {
        private readonly List<TextMeshProUGUI> _layers = new List<TextMeshProUGUI>();
        private Quaternion _baseRotation = Quaternion.identity;

        public float tiltX = 10f;
        public float tiltY = 7f;
        public float punch;
        // Slam in : comes from big and turned, lands with an overshoot.
        private float _slamT = 1f;
        private float _slamScale = 1f;
        private float _slamYaw;
        // Squash & stretch on a spring (vertical scale, volume kept).
        private float _squash = 1f;
        private float _squashVelocity;

        public RectTransform Rect => (RectTransform)transform;

        public static UI3DText Create(Transform pParent, string pText, float pSize, Vector2 pAnchor, Vector2 pPosition, Vector2 pRect, Color pFace, Color pSide,
                                      int pLayers = 12, float pDepth = 2.6f, TextAlignmentOptions pAlign = TextAlignmentOptions.Center, float pRoll = 0f)
        {
            RectTransform lRoot = UIFactory.CreateRect(pParent, "3D_" + pText, pAnchor, pPosition, pRect);
            UI3DText lWord = lRoot.gameObject.AddComponent<UI3DText>();
            lWord._baseRotation = Quaternion.Euler(0f, 0f, pRoll);
            for (int i = pLayers; i >= 0; i--)
            {
                RectTransform lRect = UIFactory.Stretch(UIFactory.CreateRect(lRoot, i == 0 ? "Face" : "Side", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
                lRect.localPosition = new Vector3(0f, 0f, i * pDepth);
                TextMeshProUGUI lText = lRect.gameObject.AddComponent<TextMeshProUGUI>();
                lText.font = GameAssets.TitleFont;
                lText.text = pText;
                lText.fontStyle = FontStyles.Italic;
                lText.fontSize = pSize;
                lText.enableAutoSizing = true;
                lText.fontSizeMin = 8f;
                lText.fontSizeMax = pSize;
                lText.alignment = pAlign;
                lText.textWrappingMode = TextWrappingModes.NoWrap;
                lText.overflowMode = TextOverflowModes.Overflow;
                lText.raycastTarget = false;
                lText.color = i == 0 ? pFace : Color.Lerp(pSide, Color.black, i / (float)pLayers * 0.4f);
                lWord._layers.Add(lText);
            }
            return lWord;
        }

        public void SetText(string pText)
        {
            foreach (TextMeshProUGUI lLayer in _layers) lLayer.text = pText;
        }

        public void SetFace(Color pColor) => _layers[_layers.Count - 1].color = pColor;

        public void SetSide(Color pColor)
        {
            for (int i = 0; i < _layers.Count - 1; i++) _layers[i].color = Color.Lerp(pColor, Color.black, (_layers.Count - 1 - i) / (float)_layers.Count * 0.4f);
        }

        public void Slam(float pFromScale = 2.4f, float pFromYaw = 60f)
        {
            _slamT = 0f;
            _slamScale = pFromScale;
            _slamYaw = pFromYaw;
        }

        // Negative squashes (lands), positive stretches.
        public void Kick(float pAmount) => _squashVelocity += pAmount;

        public void SetAlpha(float pAlpha)
        {
            foreach (TextMeshProUGUI lLayer in _layers) lLayer.alpha = pAlpha;
        }

        private void Update()
        {
            Vector2 lTilt = UIMouse.Tilt;
            float lDt = Time.unscaledDeltaTime;
            punch = Mathf.MoveTowards(punch, 0f, lDt * 3.5f);
            _slamT = Mathf.MoveTowards(_slamT, 1f, lDt / 0.3f);
            float lE = OutBack(_slamT);
            float lSlamScale = Mathf.LerpUnclamped(_slamScale, 1f, lE);
            float lSlamYaw = Mathf.LerpUnclamped(_slamYaw, 0f, lE);
            _squashVelocity += ((1f - _squash) * 260f - _squashVelocity * 13f) * lDt;
            _squash = Mathf.Clamp(_squash + _squashVelocity * lDt, 0.55f, 1.5f);
            transform.localRotation = _baseRotation * Quaternion.Euler(lTilt.y * tiltY, -lTilt.x * tiltX + lSlamYaw, -lSlamYaw * 0.2f);
            float lScale = (1f + punch * punch * 0.5f) * lSlamScale;
            transform.localScale = new Vector3(lScale / Mathf.Sqrt(_squash), lScale * _squash, lScale);
        }

        private static float OutBack(float pT)
        {
            const float C1 = 1.70158f;
            const float C3 = C1 + 1f;
            return 1f + C3 * Mathf.Pow(pT - 1f, 3f) + C1 * Mathf.Pow(pT - 1f, 2f);
        }
    }

    // A whole screen as one single panel, swinging around the centre of the screen with the mouse : everything on it moves
    // together, nothing leans on its own. Set baseEuler to stand it in perspective.
    public class PerspectivePanel : MonoBehaviour
    {
        public Vector3 baseEuler;
        public float amountX = 3.5f;
        public float amountY = 2.5f;
        public Vector2 slide = new Vector2(14f, 8f);

        private void Update()
        {
            Vector2 lTilt = UIMouse.Tilt;
            transform.localRotation = Quaternion.Euler(baseEuler.x + lTilt.y * amountY, baseEuler.y - lTilt.x * amountX, baseEuler.z);
            ((RectTransform)transform).anchoredPosition = new Vector2(-lTilt.x * slide.x, -lTilt.y * slide.y);
        }
    }
}
