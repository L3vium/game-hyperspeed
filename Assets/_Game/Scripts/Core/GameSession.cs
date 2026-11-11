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

        // ---------------- other saved settings (cached floats)
        private static readonly System.Collections.Generic.Dictionary<string, float> _floats = new System.Collections.Generic.Dictionary<string, float>();

        public static float GetSetting(string pKey, float pDefault)
        {
            float lValue;
            if (!_floats.TryGetValue(pKey, out lValue))
            {
                lValue = PlayerPrefs.GetFloat(pKey, pDefault);
                _floats[pKey] = lValue;
            }
            return lValue;
        }

        public static void SetSetting(string pKey, float pValue)
        {
            _floats[pKey] = pValue;
            PlayerPrefs.SetFloat(pKey, pValue);
            PlayerPrefs.Save();
        }

        // Right stick speed, horizontal and vertical, 0.3 to 2.5.
        public static float PadSensitivityX { get => GetSetting("hs_pad_x", 1f); set => SetSetting("hs_pad_x", Mathf.Clamp(value, 0.3f, 2.5f)); }
        public static float PadSensitivityY { get => GetSetting("hs_pad_y", 1f); set => SetSetting("hs_pad_y", Mathf.Clamp(value, 0.3f, 2.5f)); }
        // Field of view added to the camera's own, degrees (-15 to +15).
        public static float FovOffset { get => GetSetting("hs_fov", 0f); set => SetSetting("hs_fov", Mathf.Clamp(value, -15f, 15f)); }
        // Camera distance scale, 0.7 to 1.5.
        public static float CameraDistance { get => GetSetting("hs_cam_dist", 1f); set => SetSetting("hs_cam_dist", Mathf.Clamp(value, 0.7f, 1.5f)); }
        // Screen shake, chromatic aberration and flashes, 0 to 1.
        public static float ShakeAmount { get => GetSetting("hs_shake", 1f); set => SetSetting("hs_shake", Mathf.Clamp01(value)); }
        public static float AberrationAmount { get => GetSetting("hs_aberration", 1f); set => SetSetting("hs_aberration", Mathf.Clamp01(value)); }
        public static float FlashAmount { get => GetSetting("hs_flash", 1f); set => SetSetting("hs_flash", Mathf.Clamp01(value)); }
        public static bool Vibration { get => GetSetting("hs_vibration", 1f) > 0.5f; set => SetSetting("hs_vibration", value ? 1f : 0f); }
        public static bool VSync { get => GetSetting("hs_vsync", 1f) > 0.5f; set { SetSetting("hs_vsync", value ? 1f : 0f); ApplyDisplay(); } }
        public static int Quality
        {
            get => Mathf.Clamp(Mathf.RoundToInt(GetSetting("hs_quality", QualitySettings.GetQualityLevel())), 0, QualitySettings.names.Length - 1);
            set { SetSetting("hs_quality", Mathf.Clamp(value, 0, QualitySettings.names.Length - 1)); ApplyDisplay(); }
        }

        // Saved display options that Unity does not keep by itself (resolution and window mode it does).
        public static void ApplyDisplay()
        {
            if (QualitySettings.GetQualityLevel() != Quality) QualitySettings.SetQualityLevel(Quality, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : 240;
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
