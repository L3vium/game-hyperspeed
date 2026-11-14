using MoreMountains.Feedbacks;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.Feel
{
    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Hit Stop (freeze frame)")]
    [FeedbackHelp("Holds the world for a few hundredths of a second (TimeScale 0 = full freeze). The view and your inputs keep answering.")]
    public class MMF_HSHitstop : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.TimeColor;
#endif
        [MMFInspectorGroup("Hit stop", true, 47)]
        public float Duration = 0.05f;
        [Range(0f, 1f)] public float TimeScale = 0f;

        public override float FeedbackDuration { get => Duration; set => Duration = value; }

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            float lDuration = Duration * Mathf.Clamp(Strength(feedbacksIntensity, position), 0f, 3f);
            if (lDuration <= 0.001f) return;
            if (TimeScale <= 0.001f) Juice.Freeze(lDuration);
            else Juice.Hitstop(lDuration, TimeScale);
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Slow Motion")]
    [FeedbackHelp("Drops the time scale, then eases it back to normal over Duration.")]
    public class MMF_HSSlowMo : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.TimeColor;
#endif
        [MMFInspectorGroup("Slow motion", true, 47)]
        [Range(0.02f, 1f)] public float TimeScale = 0.35f;
        public float Duration = 0.2f;

        public override float FeedbackDuration { get => Duration; set => Duration = value; }

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            float lDuration = Duration * Mathf.Clamp(Strength(feedbacksIntensity, position), 0f, 3f);
            if (lDuration > 0.001f) Juice.SlowMo(TimeScale, lDuration);
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Pad Rumble")]
    [FeedbackHelp("Buzzes the gamepad (VIBRATION setting).")]
    public class MMF_HSRumble : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.HapticsColor;
#endif
        [MMFInspectorGroup("Rumble", true, 47)]
        [Range(0f, 1f)] public float Power = 0.5f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            Juice.Rumble(Power * Strength(feedbacksIntensity, position));
        }
    }
}
