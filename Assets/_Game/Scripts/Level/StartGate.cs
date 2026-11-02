using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Start arch : an energy barrier holds the runner in the plaza until GO, the countdown shows on the arch itself.
    public class StartGate : MonoBehaviour
    {
        public static StartGate Instance { get; private set; }

        private Transform _barrier;
        private Material _barrierMaterial;
        private TextMeshPro _count;
        private float _open;
        private bool _opening;
        private float _countPunch;

        public float LineS { get; private set; }

        public static StartGate Create(Transform pParent, Vector3 pPosition, Quaternion pRotation, float pWidth, float pLineS, Theme pTheme)
        {
            GameObject lObject = new GameObject("StartGate");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.SetPositionAndRotation(pPosition, pRotation);
            StartGate lGate = lObject.AddComponent<StartGate>();
            lGate.LineS = pLineS;
            Instance = lGate;
            float lHalf = pWidth * 0.5f + 1.5f;
            LevelProps.Arch(lObject.transform, lHalf, 13f, pTheme, false);

            lGate._barrier = new GameObject("Barrier").transform;
            lGate._barrier.SetParent(lObject.transform, false);
            lGate._barrier.localPosition = new Vector3(0f, 3.5f, 0f);
            Color lColor = pTheme.accent * 1.2f;
            lColor.a = 0.35f;
            lGate._barrierMaterial = new Material(Mats.Glow(lColor, true, 0f, 0f, false));
            lGate._barrierMaterial.SetFloat("_StripeFreq", 6f);
            lGate._barrierMaterial.SetFloat("_Scroll", 0.8f);
            lGate._barrierMaterial.SetFloat("_Cull", 0f);
            GameObject lQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(lQuad.GetComponent<Collider>());
            lQuad.transform.SetParent(lGate._barrier, false);
            lQuad.transform.localScale = new Vector3(lHalf * 2f, 7f, 1f);
            lQuad.GetComponent<Renderer>().sharedMaterial = lGate._barrierMaterial;

            GameObject lText = new GameObject("Count");
            lText.transform.SetParent(lObject.transform, false);
            lText.transform.localPosition = new Vector3(0f, 16.5f, -0.6f);
            lText.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            lGate._count = lText.AddComponent<TextMeshPro>();
            lGate._count.font = GameAssets.TitleFont;
            lGate._count.fontSize = 60f;
            lGate._count.alignment = TextAlignmentOptions.Center;
            lGate._count.fontStyle = FontStyles.Italic | FontStyles.Bold;
            lGate._count.color = Color.white;
            lGate._count.text = "";
            lGate._count.rectTransform.sizeDelta = new Vector2(20f, 8f);
            return lGate;
        }

        public void ShowCount(string pText, Color pColor)
        {
            _count.text = pText;
            _count.color = pColor;
            _countPunch = 1f;
        }

        public void Open()
        {
            _opening = true;
            _count.text = "GO";
            _count.color = UIKit.GOLD;
            _countPunch = 1f;
        }

        private void Update()
        {
            _countPunch = Mathf.MoveTowards(_countPunch, 0f, Time.deltaTime * 3f);
            if (_count != null) _count.transform.localScale = Vector3.one * (1f + _countPunch * _countPunch * 0.6f);
            if (_opening)
            {
                _open = Mathf.MoveTowards(_open, 1f, Time.deltaTime * 2.5f);
                _barrier.localScale = new Vector3(1f, 1f - _open, 1f);
                if (_open >= 1f) _barrier.gameObject.SetActive(false);
                if (_open >= 1f && _count != null) _count.alpha = Mathf.MoveTowards(_count.alpha, 0f, Time.deltaTime * 2f);
            }
            else _barrierMaterial.SetFloat("_Scroll", 0.8f + Mathf.Sin(Time.time * 3f) * 0.2f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
