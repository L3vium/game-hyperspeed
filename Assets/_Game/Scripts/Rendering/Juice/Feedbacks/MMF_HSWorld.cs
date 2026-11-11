using MoreMountains.Feedbacks;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.Feel
{
    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Sound")]
    [FeedbackHelp("Plays a sound from Resources/Audio (drag a clip, its name is the id). Several clips play in turn (round robin) so a repeated action never sounds the same twice. Follows the EFFECTS volume.")]
    public class MMF_HSSound : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.SoundsColor;
        public override bool EvaluateRequiresSetup() => (Clips == null || Clips.Length == 0) && string.IsNullOrEmpty(ClipId);
        public override string RequiredTargetText => Clips != null && Clips.Length > 0 && Clips[0] != null ? Clips[0].name : ClipId;
#endif
        [MMFInspectorGroup("Sound", true, 14, true)]
        [Tooltip("Clips from Assets/_Game/Resources/Audio, played in turn.")]
        public AudioClip[] Clips;
        [Tooltip("Or a clip name in Resources/Audio.")]
        public string ClipId;
        [Range(0f, 1.5f)] public float Volume = 0.9f;
        public float Pitch = 1f;
        [Range(0f, 0.3f)] public float PitchVariance = 0.05f;
        [Tooltip("The event's intensity also raises the pitch a little.")]
        public float PitchFromIntensity = 0f;

        private int _next;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            string lId = ClipId;
            if (Clips != null && Clips.Length > 0)
            {
                AudioClip lClip = Clips[_next % Clips.Length];
                _next++;
                if (lClip != null) lId = lClip.name;
            }
            if (string.IsNullOrEmpty(lId)) return;
            float lK = Strength(feedbacksIntensity, position);
            AudioManager.Play(lId, Mathf.Clamp(Volume * lK, 0f, 1.5f), Pitch + PitchFromIntensity * Mathf.Clamp01(feedbacksIntensity), PitchVariance);
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Music Duck")]
    [FeedbackHelp("Pulls the music down (and muffles it) for a moment, so a big hit stands out.")]
    public class MMF_HSDuck : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.SoundsColor;
#endif
        [MMFInspectorGroup("Duck", true, 14)]
        public float Duration = 0.4f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            AudioManager.Duck(Duration * Mathf.Clamp(Strength(feedbacksIntensity, position), 0f, 3f));
        }
    }

    public enum HSFXKind { Sparks, Burst, Shockwave, Shards, Puff, Explosion, Streak }
    public enum HSDirection { ContextDirection, ContextNormal, WorldUp, TowardCamera, Random }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/World FX (sparks, shockwave, debris...)")]
    [FeedbackHelp("Spawns one of the game's procedural effects at the event : sparks, a burst, a shockwave ring, shards, a dust puff, an explosion or a streak. Count and size scale with the intensity.")]
    public class MMF_HSFX : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.ParticlesColor;
#endif
        [MMFInspectorGroup("Effect", true, 23)]
        public HSFXKind Kind = HSFXKind.Sparks;
        public HSColorSource ColorSource = HSColorSource.Fixed;
        public Color EffectColor = Color.white;
        public HSDirection Direction = HSDirection.ContextNormal;
        [Tooltip("Particles (sparks, shards, puff).")]
        public int Count = 30;
        [Tooltip("Size in metres (shockwave diameter, burst size, particle size for sparks...).")]
        public float Size = 20f;
        [Tooltip("Speed of the particles.")]
        public float Speed = 40f;
        [Tooltip("Life (sparks) or duration (shockwave, streak).")]
        public float Life = 0.4f;
        [Tooltip("Offset from the event, along the event's normal.")]
        public float Lift = 0.5f;
        [Tooltip("Size also grows with the event's size (rings, blocks).")]
        public bool UseContextSize;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            float lK = Strength(feedbacksIntensity, position);
            if (lK <= 0.001f) return;
            Vector3 lNormal = Normal;
            Vector3 lAt = Where(position) + lNormal * Lift;
            Color lColor = HSColors.Pick(ColorSource, EffectColor, Ctx);
            Vector3 lDir;
            switch (Direction)
            {
                case HSDirection.ContextDirection: lDir = Dir; break;
                case HSDirection.WorldUp: lDir = Vector3.up; break;
                case HSDirection.TowardCamera: lDir = CameraRig.Instance != null ? (CameraRig.Instance.transform.position - lAt).normalized : Vector3.up; break;
                case HSDirection.Random: lDir = Random.onUnitSphere; break;
                default: lDir = lNormal; break;
            }
            float lSize = Size * lK * (UseContextSize ? Mathf.Max(0.1f, Ctx.size) : 1f);
            int lCount = Mathf.Max(1, Mathf.RoundToInt(Count * Mathf.Clamp(lK, 0.2f, 3f)));
            switch (Kind)
            {
                case HSFXKind.Sparks: FX.Sparks(lAt, lColor, lCount, Speed * Mathf.Sqrt(lK), Mathf.Max(0.05f, Size * 0.05f), Life); break;
                case HSFXKind.Burst: FX.Burst(lAt, lDir, lColor, lSize); break;
                case HSFXKind.Shockwave: FX.Shockwave(lAt, lDir, lColor, lSize, Life); break;
                case HSFXKind.Shards: FX.Shards(lAt, lColor, lCount, Speed, Mathf.Max(0.2f, Size * 0.1f), lDir * Speed * 0.5f); break;
                case HSFXKind.Puff: FX.Puff(lAt, lColor, lCount, Mathf.Max(0.5f, Size * 0.2f), Speed * 0.2f); break;
                case HSFXKind.Explosion: FX.Explosion(lAt, lColor, lSize * 0.05f); break;
                case HSFXKind.Streak: FX.Streak(lAt, lDir, Speed, Life, lColor); break;
            }
        }
    }

    [AddComponentMenu(""), System.Serializable, FeedbackPath("Hyperspeed/Runner Squash and Stretch")]
    [FeedbackHelp("Kicks the runner's body springs : Up stretches it tall (negative squashes), Forward stretches it along the run. Flash lights the body white.")]
    public class MMF_HSBody : MMF_HSFeedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.TransformColor;
#endif
        [MMFInspectorGroup("Body", true, 50)]
        public float Up = 0f;
        public float Forward = 2.5f;
        [Range(0f, 1f)] public float Flash = 0.4f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!CanRun || !FeedbackTypeAuthorized) return;
            PlayerController lPlayer = RaceContext.Player;
            if (lPlayer == null || lPlayer.Visual == null) return;
            float lK = Strength(feedbacksIntensity, position);
            lPlayer.Visual.Punch(Up * lK, Forward * lK, Flash);
        }
    }
}
