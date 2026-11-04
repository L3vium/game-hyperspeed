using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Dev build only : the digit-key cheats, small, bottom-right. A key lights up while its cheat is on.
    public class DevCheatBar : MonoBehaviour
    {
        private static readonly string[] LABELS = { "GOD", "BOOST", "SKIP", "WIN", "SLOW", "NEXT", "BOT", "MAX", "ROCK", "RETRY" };
        private static readonly string[] KEYS = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private const float CELL = 44f;

        private TextMeshProUGUI[] _digits;
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
            _digits = new TextMeshProUGUI[KEYS.Length];
            for (int i = 0; i < KEYS.Length; i++)
            {
                Vector2 lPosition = new Vector2(-CELL * (KEYS.Length - i) + CELL * 0.5f - 14f, 38f);
                _digits[i] = UIKit.Text(pRoot, KEYS[i], 22f, new Color(1f, 1f, 1f, 0.55f), new Vector2(1f, 0f), lPosition, new Vector2(CELL, 26f), UIKit.Font.Heavy, TextAlignmentOptions.Center, false);
                TextMeshProUGUI lLabel = UIKit.Text(pRoot, LABELS[i], 11f, new Color(1f, 1f, 1f, 0.4f), new Vector2(1f, 0f), lPosition + new Vector2(0f, -20f), new Vector2(CELL, 16f), UIKit.Font.Bold, TextAlignmentOptions.Center, false);
                UIFactory.Outline(lLabel, UIKit.INK, 0.2f);
                UIFactory.Outline(_digits[i], UIKit.INK, 0.2f);
            }
        }

        public void SetActive(int pIndex, bool pValue) => _on[pIndex] = pValue;

        public void Flash(int pIndex) => _flash[pIndex] = 0.25f;

        private void Update()
        {
            for (int i = 0; i < _digits.Length; i++)
            {
                _flash[i] -= Time.unscaledDeltaTime;
                _digits[i].color = _on[i] || _flash[i] > 0f ? UIKit.GOLD : new Color(1f, 1f, 1f, 0.55f);
            }
        }
    }
}
