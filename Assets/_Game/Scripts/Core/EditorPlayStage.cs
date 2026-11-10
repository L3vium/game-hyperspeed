#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // "Play this stage" from the Level Design window : play mode starts on the title, and this sends it straight into the
    // chosen stage (optionally on autopilot).
    public static class EditorPlayStage
    {
        public const string KEY_STAGE = "HS_LD_PlayStage";
        public const string KEY_AUTOPILOT = "HS_LD_PlayAutopilot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            int lStage = EditorPrefs.GetInt(KEY_STAGE, -1);
            if (lStage < 0) return;
            EditorPrefs.DeleteKey(KEY_STAGE);
            bool lAutopilot = EditorPrefs.GetBool(KEY_AUTOPILOT, false);
            GameSession.CheatsEnabled = true;
            GameSession.SelectedLevel = lStage;
            Autopilot.Enabled = lAutopilot;
            SceneTransition.LoadStage(lStage);
        }
    }
}
#endif
