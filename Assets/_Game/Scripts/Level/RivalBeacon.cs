using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Makes a rival read at any distance : a soft glow around it up close, a four-pointed star when it is far away.
    public class RivalBeacon : MonoBehaviour
    {
        private Transform _halo;
        private Transform _star;
        private Material _starMaterial;
        private Material _haloMaterial;
        private float _size;

        public static RivalBeacon Attach(Transform pRival, Transform pModel)
        {
            Bounds lBounds = new Bounds(pModel.position, Vector3.one);
            foreach (Renderer lRenderer in pModel.GetComponentsInChildren<Renderer>()) lBounds.Encapsulate(lRenderer.bounds);
            RivalBeacon lBeacon = pRival.gameObject.AddComponent<RivalBeacon>();
            lBeacon._size = Mathf.Max(3f, lBounds.extents.magnitude);
            Vector3 lCenter = pRival.InverseTransformPoint(lBounds.center);
            lBeacon._haloMaterial = new Material(Mats.Glow(new Color(1.6f, 1.2f, 2.6f, 0.35f), true, 1f, 1f));
            lBeacon._halo = Mats.Primitive(PrimitiveType.Sphere, pRival, lCenter, Vector3.one * lBeacon._size * 2.2f, lBeacon._haloMaterial, "Halo").transform;

            // The star : two thin crossed quads facing the camera.
            GameObject lStar = new GameObject("Star");
            lStar.transform.SetParent(pRival, false);
            lStar.transform.localPosition = lCenter;
            MeshData lData = new MeshData();
            lData.Quad(new Vector3(-1f, -0.06f, 0f), new Vector3(1f, -0.06f, 0f), new Vector3(1f, 0.06f, 0f), new Vector3(-1f, 0.06f, 0f), Vector3.back, Color.white);
            lData.Quad(new Vector3(-0.06f, -1f, 0f), new Vector3(0.06f, -1f, 0f), new Vector3(0.06f, 1f, 0f), new Vector3(-0.06f, 1f, 0f), Vector3.back, Color.white);
            lData.Quad(new Vector3(-0.22f, -0.22f, 0f), new Vector3(0.22f, -0.22f, 0f), new Vector3(0.22f, 0.22f, 0f), new Vector3(-0.22f, 0.22f, 0f), Vector3.back, Color.white);
            lStar.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("RivalStar");
            lBeacon._starMaterial = new Material(Mats.Glow(new Color(3f, 2.6f, 3.4f, 1f), true, 0f));
            MeshRenderer lRenderer2 = lStar.AddComponent<MeshRenderer>();
            lRenderer2.sharedMaterial = lBeacon._starMaterial;
            lRenderer2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lBeacon._star = lStar.transform;
            return lBeacon;
        }

        private void LateUpdate()
        {
            Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            if (lCamera == null || _star == null) return;
            float lDistance = Vector3.Distance(lCamera.transform.position, _star.position);
            float lFar = Mathf.InverseLerp(_size * 25f, _size * 90f, lDistance);
            _star.rotation = Quaternion.LookRotation(_star.position - lCamera.transform.position, lCamera.transform.up) * Quaternion.Euler(0f, 0f, Time.time * 40f);
            float lPulse = 1f + Mathf.Sin(Time.time * 9f) * 0.15f;
            _star.localScale = Vector3.one * lDistance * 0.035f * lPulse * Mathf.Lerp(0.3f, 1f, lFar);
            _starMaterial.SetColor("_Color", new Color(3f, 2.6f, 3.4f, Mathf.Lerp(0.15f, 1f, lFar)));
            _haloMaterial.SetColor("_Color", new Color(1.6f, 1.2f, 2.6f, 0.35f * (1f - lFar * 0.7f) * (0.85f + Mathf.Sin(Time.time * 6f) * 0.15f)));
        }
    }
}
