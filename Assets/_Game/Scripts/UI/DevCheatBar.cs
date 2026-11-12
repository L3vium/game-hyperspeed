using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Dev build only : the digit-key cheats as a readable vertical list on the right edge, one row per key
    // (key cap + name). A row lights up gold while its cheat is on, or flashes when it fires.
    public class DevCheatBar : MonoBehaviour
    {
        private static readonly string[] LABELS = { "GOD MODE", "BOOST", "SKIP AHEAD", "WIN", "SLOW MOTION", "NEXT STAGE", "AUTOPILOT", "MAX SPEED", "METEOR", "RETRY" };
        private static readonly string[] KEYS = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private const float ROW = 38f;
        private const float WIDTH = 250f;
        private const float RIGHT = 24f;

        private static readonly Color IDLE_KEY = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color IDLE_LABEL = new Color(1f, 1f, 1f, 0.72f);
        private static readonly Color CAP = new Color(1f, 1f, 1f, 0.12f);

        private TextMeshProUGUI[] _digits;
        private TextMeshProUGUI[] _labels;
        private UIShape[] _caps;
        private readonly bool[] _on = new bool[10];
        private readonly float[] _flash = new float[10];

        public static DevCheatBar Create()
        {
            Canvas lCanvas = UIFactory.CreateCanvas("DevCheats", 900);
            DevCheatBar lBar = lCanvas.gameObject.AddComponent<DevCheatBar>();
            lBar.Build(lCanvas.transform);
            return lBar;
        }

        private void Build(Transform pRoot)
        {
            int lCount = KEYS.Length;
            float lHeight = ROW * lCount + 56f;
            Vector2 lAnchor = new Vector2(1f, 0.5f);
            float lCenterX = -RIGHT - WIDTH * 0.5f;

            UIShape lPanel = UIKit.Tile(pRoot, "CheatPanel", new Color(UIKit.INK.r, UIKit.INK.g, UIKit.INK.b, 0.72f), lAnchor, new Vector2(lCenterX, 0f), new Vector2(WIDTH, lHeight), 0f);
            lPanel.raycastTarget = false;
            TextMeshProUGUI lTitle = UIKit.Text(pRoot, "DEV CHEATS", 18f, UIKit.GOLD, lAnchor, new Vector2(lCenterX, lHeight * 0.5f - 24f), new Vector2(WIDTH - 24f, 26f), UIKit.Font.Heavy, TextAlignmentOptions.Center, false);
            lTitle.raycastTarget = false;

            _digits = new TextMeshProUGUI[lCount];
            _labels = new TextMeshProUGUI[lCount];
            _caps = new UIShape[lCount];
            float lTop = lHeight * 0.5f - 56f - ROW * 0.5f;
            float lLeft = lCenterX - WIDTH * 0.5f;
            for (int i = 0; i < lCount; i++)
            {
                float lY = lTop - ROW * i;
                Vector2 lCapPosition = new Vector2(lLeft + 16f + 16f, lY);
                _caps[i] = UIKit.Tile(pRoot, "Key" + KEYS[i], CAP, lAnchor, lCapPosition, new Vector2(32f, 30f), 0f);
                _caps[i].raycastTarget = false;
                _digits[i] = UIKit.Text(pRoot, KEYS[i], 22f, IDLE_KEY, lAnchor, lCapPosition, new Vector2(32f, 30f), UIKit.Font.Heavy, TextAlignmentOptions.Center, false);
                _labels[i] = UIKit.Text(pRoot, LABELS[i], 18f, IDLE_LABEL, lAnchor, new Vector2(lLeft + 60f + (WIDTH - 72f) * 0.5f, lY), new Vector2(WIDTH - 72f, 30f), UIKit.Font.Bold, TextAlignmentOptions.MidlineLeft, false);
                _digits[i].raycastTarget = false;
                _labels[i].raycastTarget = false;
                UIFactory.Outline(_digits[i], UIKit.INK, 0.2f);
            }
        }

        public void SetActive(int pIndex, bool pValue) => _on[pIndex] = pValue;

        public void Flash(int pIndex) => _flash[pIndex] = 0.35f;

        private void Update()
        {
            for (int i = 0; i < _digits.Length; i++)
            {
                _flash[i] -= Time.unscaledDeltaTime;
                bool lLit = _on[i] || _flash[i] > 0f;
                _digits[i].color = lLit ? UIKit.INK : IDLE_KEY;
                _labels[i].color = lLit ? UIKit.GOLD : IDLE_LABEL;
                _caps[i].color = lLit ? UIKit.GOLD : CAP;
            }
        }
    }
}
