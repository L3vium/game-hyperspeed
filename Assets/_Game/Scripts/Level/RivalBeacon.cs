using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Makes a rival read at any distance : a soft glow around it up close, and from afar a round pin floating above it with
    // its name and how far ahead or behind it is. Always the same size on screen, always facing the camera.
    public class RivalBeacon : MonoBehaviour
    {
        private const float PIN_SCREEN = 0.03f;

        private Transform _halo;
        private Transform _pin;
        private Material _pinMaterial;
        private Material _haloMaterial;
        private TextMeshPro _label;
        private Rival _rival;
        private string _name;
        private float _size;
        private float _lift;

        public static RivalBeacon Attach(Transform pRival, Transform pModel)
        {
            Bounds lBounds = new Bounds(pModel.position, Vector3.one);
            foreach (Renderer lRenderer in pModel.GetComponentsInChildren<Renderer>()) lBounds.Encapsulate(lRenderer.bounds);
            RivalBeacon lBeacon = pRival.gameObject.AddComponent<RivalBeacon>();
            lBeacon._rival = pRival.GetComponent<Rival>();
            lBeacon._size = Mathf.Max(3f, lBounds.extents.magnitude);
            Vector3 lCenter = pRival.InverseTransformPoint(lBounds.center);
            lBeacon._haloMaterial = new Material(Mats.Glow(new Color(1.6f, 1.2f, 2.6f, 0.35f), true, 1f, 1f));
            lBeacon._halo = Mats.Primitive(PrimitiveType.Sphere, pRival, lCenter, Vector3.one * lBeacon._size * 2.2f, lBeacon._haloMaterial, "Halo").transform;

            // The pin : a round badge with a point under it, unit size, scaled with the distance.
            GameObject lPin = new GameObject("Pin");
            lPin.transform.SetParent(pRival, false);
            lBeacon._lift = lBeacon._size * 1.3f;
            lPin.transform.localPosition = lCenter + Vector3.up * lBeacon._lift;
            MeshData lData = new MeshData();
            const int SIDES = 28;
            for (int i = 0; i < SIDES; i++)
            {
                float lA = i / (float)SIDES * Mathf.PI * 2f;
                float lB = (i + 1) / (float)SIDES * Mathf.PI * 2f;
                Vector3 lOuterA = new Vector3(Mathf.Cos(lA), Mathf.Sin(lA), 0f) * 0.5f;
                Vector3 lOuterB = new Vector3(Mathf.Cos(lB), Mathf.Sin(lB), 0f) * 0.5f;
                // Ring and a smaller full disc in the middle.
                lData.FacetQuad(lOuterA, lOuterB, lOuterB * 0.78f, lOuterA * 0.78f, Vector3.back, Color.white);
                lData.FacetTri(Vector3.zero, lOuterA * 0.5f, lOuterB * 0.5f, Vector3.back, Color.white);
            }
            lData.FacetTri(new Vector3(-0.16f, -0.44f, 0f), new Vector3(0.16f, -0.44f, 0f), new Vector3(0f, -0.82f, 0f), Vector3.back, Color.white);
            lPin.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("RivalPin");
            lBeacon._pinMaterial = new Material(Mats.Glow(new Color(3f, 1.1f, 1.4f, 1f), true, 0f));
            MeshRenderer lRenderer2 = lPin.AddComponent<MeshRenderer>();
            lRenderer2.sharedMaterial = lBeacon._pinMaterial;
            lRenderer2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lBeacon._pin = lPin.transform;

            // Its name and the gap, under the badge.
            GameObject lText = new GameObject("Label");
            lText.transform.SetParent(lPin.transform, false);
            lText.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            lBeacon._label = lText.AddComponent<TextMeshPro>();
            lBeacon._label.font = GameAssets.TitleFont;
            lBeacon._label.fontSize = 4f;
            lBeacon._label.fontStyle = FontStyles.Italic | FontStyles.Bold;
            lBeacon._label.alignment = TextAlignmentOptions.Center;
            lBeacon._label.textWrappingMode = TextWrappingModes.NoWrap;
            lBeacon._label.outlineWidth = 0.25f;
            lBeacon._label.outlineColor = new Color32(20, 10, 24, 255);
            lBeacon._label.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
            lBeacon._label.color = Color.white;
            lBeacon._name = lBeacon._rival != null ? RivalFactory.DisplayName(lBeacon._rival.kind).Replace("THE ", "") : "";
            lText.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lBeacon;
        }

        private void LateUpdate()
        {
            Camera lCamera = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            if (lCamera == null || _pin == null) return;
            float lDistance = Vector3.Distance(lCamera.transform.position, _pin.position);
            float lFar = Mathf.InverseLerp(_size * 12f, _size * 40f, lDistance);
            _pin.rotation = Quaternion.LookRotation(_pin.position - lCamera.transform.position, lCamera.transform.up);
            float lPulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
            _pin.localScale = Vector3.one * lDistance * PIN_SCREEN * lPulse * Mathf.Lerp(0.6f, 1f, lFar);
            _pinMaterial.SetColor("_Color", new Color(3f, 1.1f, 1.4f, Mathf.Lerp(0.25f, 1f, lFar)));
            if (_label != null)
            {
                float lGap = _rival != null ? _rival.distance - RaceContext.PlayerProgress : 0f;
                string lWhere = _rival != null && _rival.Finished ? "FINISHED" : (lGap >= 0f ? "+" : "-") + Mathf.RoundToInt(Mathf.Abs(lGap)) + " m";
                _label.text = _name + "  " + lWhere;
                _label.alpha = Mathf.Lerp(0f, 1f, lFar);
            }
            _haloMaterial.SetColor("_Color", new Color(1.6f, 1.2f, 2.6f, 0.35f * (1f - lFar * 0.7f) * (0.85f + Mathf.Sin(Time.time * 6f) * 0.15f)));
        }
    }
}
