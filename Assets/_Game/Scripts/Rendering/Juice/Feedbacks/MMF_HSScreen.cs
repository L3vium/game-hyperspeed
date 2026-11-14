using MoreMountains.Feedbacks;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.Feel
{
    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Screen Kick (aberration, blur, bloom...)")]
    [FeedbackHelp("A burst of post process that fades over Duration : chromatic aberration, radial speed blur, bloom, exposure, barrel distortion, saturation. Adds on top of the speed look. Aberration follows the ABERRATION setting.")]
    public class MMF_HSScreenKick : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.PostProcessColor;
#endif
        [MMFInspectorGroup("Screen kick", true, 46)]
        [Tooltip("Chromatic aberration (0..3 is already a lot).")]
        public float Aberration = 1f;
        [Tooltip("Radial speed blur toward the edges.")]
        public float RadialBlur = 0.6f;
        public float Bloom = 0.6f;
        [Tooltip("Exposure in stops.")]
        public float Exposure = 0.2f;
        [Tooltip("Lens barrel (fisheye-like bulge) amount, 0..1.")]
        public float Lens = 0.25f;
        [Tooltip("Saturation added (in the post process's -100..100 units).")]
        public float Saturation = 15f;
        public float Duration = 0.45f;
        [Tooltip("Shape of the fade (1 at start, 0 at the end).")]
        public AnimationCurve Fade = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        public override float FeedbackDuration { get => ApplyTimeMultiplier(Duration); set => Duration = value; }

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized || PostFX.Instance == null) return;
            float lK = Strength(feedbacksIntensity, position);
            PostFX.Instance.Kick(new PostKick
            {
                aberration = Aberration * lK * GameSession.AberrationAmount,
                radial = RadialBlur * lK,
                bloom = Bloom * lK,
                exposure = Exposure * lK,
                lens = Lens * lK,
                saturation = Saturation * lK,
                duration = Mathf.Max(0.02f, Duration),
                fade = Fade
            });
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Screen Flash")]
    [FeedbackHelp("Flashes the whole screen with a colour that fades quickly (FLASHES setting).")]
    public class MMF_HSFlash : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.PostProcessColor;
#endif
        [MMFInspectorGroup("Flash", true, 46)]
        public HSColorSource ColorSource = HSColorSource.Fixed;
        public Color FlashColor = Color.white;
        [Range(0f, 1f)] public float Opacity = 0.5f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            Juice.Flash(HSColors.Pick(ColorSource, FlashColor, Ctx), Mathf.Clamp01(Opacity * Strength(feedbacksIntensity, position)));
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Screen Vignette")]
    [FeedbackHelp("Tints and closes the edges of the screen for a moment (soft falloff).")]
    public class MMF_HSVignette : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.PostProcessColor;
#endif
        [MMFInspectorGroup("Vignette", true, 46)]
        public HSColorSource ColorSource = HSColorSource.Fixed;
        public Color VignetteColor = new Color(0.25f, 0.95f, 1f);
        [Range(0f, 1.5f)] public float Amount01 = 0.6f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            Juice.Vignette(HSColors.Pick(ColorSource, VignetteColor, Ctx), Amount01 * Strength(feedbacksIntensity, position));
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Impact Frame")]
    [FeedbackHelp("Anime impact frame : the scene is inked in the colours of the place for a few frames (inverted first), manga lines, and the Vefects sprite, either set in the world at the event (a broken block) or on the camera.")]
    public class MMF_HSImpactFrame : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.PostProcessColor;
#endif
        [MMFInspectorGroup("Impact frame", true, 46)]
        public ImpactReason Reason = ImpactReason.Smash;
        [Tooltip("Set in the world at the event's position (else on the camera).")]
        public bool InWorld = true;
        [Range(1, 6)] public int InkFrames = 2;
        [Range(0f, 1.5f)] public float Strength01 = 0.8f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            Vector3? lWorld = InWorld ? (Vector3?)Where(position) : null;
            Impacts.Request(Reason, lWorld, Strength01 * Strength(feedbacksIntensity, position), InkFrames);
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Speed Lines Burst")]
    [FeedbackHelp("Snaps the screen speed lines toward the centre, then they ease back out.")]
    public class MMF_HSSpeedLines : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.PostProcessColor;
#endif
        [MMFInspectorGroup("Speed lines", true, 46)]
        [Range(0f, 1f)] public float Burst = 1f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            HUD.LinesBurst(Burst * Strength(feedbacksIntensity, position));
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Popup Word")]
    [FeedbackHelp("A short word next to the speedometer. {label} is replaced by the event's label.")]
    public class MMF_HSPopup : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.UIColor;
#endif
        [MMFInspectorGroup("Popup", true, 46)]
        public string Text = "{label}";
        public Color TextColor = new Color(0.25f, 0.95f, 1f);

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            string lText = Text.Replace("{label}", Ctx.label ?? "");
            if (!string.IsNullOrEmpty(lText.Trim())) Juice.Popup(lText, TextColor, 1f);
        }
    }
}
