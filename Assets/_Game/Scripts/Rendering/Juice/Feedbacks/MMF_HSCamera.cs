using MoreMountains.Feedbacks;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.Feel
{
    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Camera Shake")]
    [FeedbackHelp("Adds trauma to the race camera (shake grows with trauma squared), scaled by the SCREEN SHAKE setting. Can buzz the pad too (VIBRATION setting).")]
    public class MMF_HSShake : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.CameraColor;
#endif
        [MMFInspectorGroup("Shake", true, 37)]
        [Range(0f, 1.5f)] public float Trauma = 0.3f;
        public bool AlsoRumble = true;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            float lAmount = Trauma * Strength(feedbacksIntensity, position);
            if (CameraRig.Instance != null) CameraRig.Instance.AddTrauma(lAmount * GameSession.ShakeAmount);
            if (AlsoRumble) Juice.Rumble(lAmount);
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Camera FOV Kick")]
    [FeedbackHelp("Widens the race camera's field of view for a moment (degrees), with a fast attack and a soft release.")]
    public class MMF_HSFovKick : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.CameraColor;
#endif
        [MMFInspectorGroup("FOV", true, 37)]
        public float Degrees = 10f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized || CameraRig.Instance == null) return;
            CameraRig.Instance.FovKick(Degrees * Strength(feedbacksIntensity, position));
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Camera Pull Back")]
    [FeedbackHelp("Throws the race camera back (metres), then its spring brings it home : the runner shoots away from the view.")]
    public class MMF_HSRollback : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.CameraColor;
#endif
        [MMFInspectorGroup("Pull back", true, 37)]
        public float Meters = 10f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized || CameraRig.Instance == null) return;
            CameraRig.Instance.Rollback(Meters * Strength(feedbacksIntensity, position));
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Camera Punch")]
    [FeedbackHelp("Kicks the camera rotation in a direction (pitch, yaw, roll degrees) and lets it spring back : a hit you feel in the view.")]
    public class MMF_HSCameraPunch : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.CameraColor;
#endif
        [MMFInspectorGroup("Punch", true, 37)]
        public Vector3 Degrees = new Vector3(-3f, 0f, 4f);
        [Tooltip("Randomly flips the roll left or right.")]
        public bool RandomRollSide = true;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized || CameraRig.Instance == null) return;
            Vector3 lPunch = Degrees * Strength(feedbacksIntensity, position) * GameSession.ShakeAmount;
            if (RandomRollSide && Random.value < 0.5f) lPunch.z = -lPunch.z;
            CameraRig.Instance.Punch(lPunch);
        }
    }
}
