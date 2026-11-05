using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Start gate : a colossal arch over the whole plaza, energy barrier until GO, the countdown in 3D letters above it.
    public class StartGate : MonoBehaviour
    {
        public static StartGate Instance { get; private set; }

        private Transform _barrier;
        private GameObject _limit;
        private Material _barrierMaterial;
        private Transform _count;
        private float _open;
        private bool _opening;
        private float _countPunch;

        public static StartGate Create(Transform pParent, LevelLayout pLayout, Theme pTheme)
        {
            float lHalf = pLayout.field.TotalHalfWidth(pLayout.startLineS - 2f) + 6f;
            float lHeight = 46f;
            Transform lRoot = GateKit.Portal(pParent, pLayout, 0f, lHalf, lHeight, pTheme.accent, Color.white, "");
            StartGate lGate = lRoot.gameObject.AddComponent<StartGate>();
            Instance = lGate;

            lGate._barrier = new GameObject("Barrier").transform;
            lGate._barrier.SetParent(lRoot, false);
            lGate._barrier.localPosition = new Vector3(0f, lHeight * 0.5f, 0f);
            Color lColor = pTheme.accent * 1.2f;
            lColor.a = 0.22f;
            lGate._barrierMaterial = new Material(Mats.Glow(lColor, true, 0f, 0f, false));
            lGate._barrierMaterial.SetFloat("_StripeFreq", 0.4f);
            lGate._barrierMaterial.SetFloat("_Scroll", 0.8f);
            lGate._barrierMaterial.SetFloat("_Cull", 0f);
            GameObject lQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(lQuad.GetComponent<Collider>());
            lQuad.transform.SetParent(lGate._barrier, false);
            lQuad.transform.localScale = new Vector3(lHalf * 2f, lHeight, 1f);
            lQuad.GetComponent<Renderer>().sharedMaterial = lGate._barrierMaterial;

            // The limit you can run up to during the count : a bright band across the ground.
            GameObject lLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(lLine.GetComponent<Collider>());
            lLine.transform.SetParent(lRoot, false);
            lLine.transform.localPosition = new Vector3(0f, 0.2f, -3f);
            lLine.transform.localScale = new Vector3(lHalf * 2f, 0.3f, 2.4f);
            lLine.GetComponent<Renderer>().sharedMaterial = Mats.Glow(pTheme.accent * 3f, false, 0f, 0f, true);
            lGate._limit = lLine;

            lGate._count = Text3D.Create(lRoot, " ", lHeight * 0.62f, lHalf * 1.4f, Color.white, Color.Lerp(pTheme.accent, Color.black, 0.5f), 14, 0.02f);
            lGate._count.localPosition = new Vector3(0f, lHeight * 1.55f, 0f);
            return lGate;
        }

        public void ShowCount(string pText, Color pColor)
        {
            Text3D.SetText(_count, pText, pColor);
            _countPunch = 1f;
        }

        public void Open()
        {
            _opening = true;
            if (_limit != null) _limit.SetActive(false);
            Text3D.SetText(_count, "GO", UIKit.GOLD);
            _countPunch = 1.5f;
        }

        private void Update()
        {
            _countPunch = Mathf.MoveTowards(_countPunch, 0f, Time.deltaTime * 2.6f);
            if (_count != null)
            {
                _count.localScale = Vector3.one * (1f + _countPunch * _countPunch * 0.45f);
                // The letters lean with the mouse, like every big title.
                Vector2 lTilt = UIMouse.Tilt;
                _count.localRotation = Quaternion.Euler(lTilt.y * -16f, lTilt.x * 22f, -4f);
            }
            if (_opening)
            {
                _open = Mathf.MoveTowards(_open, 1f, Time.deltaTime * 2.5f);
                _barrier.localScale = new Vector3(1f, 1f - _open, 1f);
                if (_open >= 1f) _barrier.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
