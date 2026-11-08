using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Cross-scene state and save data (PlayerPrefs).
    public static class GameSession
    {
        private const string UNLOCKED_KEY = "hs_unlocked";
        private const string BEST_KEY = "hs_best_";
        private const string RANK_KEY = "hs_rank_";

        public static int SelectedLevel = 0;

        // Look settings, saved. Mouse up looks up unless inverted.
        private static float _mouseSensitivity = -1f;
        private static int _invertY = -1;

        public static float MouseSensitivity
        {
            get
            {
                if (_mouseSensitivity < 0f) _mouseSensitivity = PlayerPrefs.GetFloat("hs_mouse_sens", 1f);
                return _mouseSensitivity;
            }
            set
            {
                _mouseSensitivity = Mathf.Clamp(value, 0.2f, 3f);
                PlayerPrefs.SetFloat("hs_mouse_sens", _mouseSensitivity);
                PlayerPrefs.Save();
            }
        }

        public static bool InvertY
        {
            get
            {
                if (_invertY < 0) _invertY = PlayerPrefs.GetInt("hs_invert_y", 0);
                return _invertY == 1;
            }
            set
            {
                _invertY = value ? 1 : 0;
                PlayerPrefs.SetInt("hs_invert_y", _invertY);
                PlayerPrefs.Save();
            }
        }

#if HS_CHEATS
        public static bool CheatsEnabled = true;
#else
        public static bool CheatsEnabled = false;
#endif

        public static int UnlockedCount => Mathf.Max(1, PlayerPrefs.GetInt(UNLOCKED_KEY, 1));

        public static bool IsUnlocked(int pIndex) => CheatsEnabled || pIndex < UnlockedCount;

        public static float GetBestTime(int pIndex) => PlayerPrefs.GetFloat(BEST_KEY + pIndex, -1f);

        // 0 = none, 1 = C, 2 = B, 3 = A, 4 = S
        public static int GetRank(int pIndex) => PlayerPrefs.GetInt(RANK_KEY + pIndex, 0);

        public static bool RegisterWin(int pIndex, float pTime, int pRank)
        {
            bool lRecord = false;
            float lBest = GetBestTime(pIndex);
            if (lBest < 0f || pTime < lBest)
            {
                PlayerPrefs.SetFloat(BEST_KEY + pIndex, pTime);
                lRecord = true;
            }
            if (pRank > GetRank(pIndex)) PlayerPrefs.SetInt(RANK_KEY + pIndex, pRank);
            if (pIndex + 2 > UnlockedCount) PlayerPrefs.SetInt(UNLOCKED_KEY, Mathf.Min(pIndex + 2, LevelDatabase.Count));
            PlayerPrefs.Save();
            return lRecord;
        }

        public static string RankLetter(int pRank)
        {
            switch (pRank)
            {
                case 4: return "S";
                case 3: return "A";
                case 2: return "B";
                case 1: return "C";
                default: return "-";
            }
        }

        public static string FormatTime(float pTime)
        {
            if (pTime < 0f) return "--:--.--";
            int lMinutes = (int)(pTime / 60f);
            float lSeconds = pTime - lMinutes * 60f;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00.00}", lMinutes, lSeconds);
        }
    }
}
