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
        public static bool SkipTitleIntro = false;

        public static int UnlockedCount => Mathf.Max(1, PlayerPrefs.GetInt(UNLOCKED_KEY, 1));

        public static bool IsUnlocked(int pIndex) => pIndex < UnlockedCount;

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
