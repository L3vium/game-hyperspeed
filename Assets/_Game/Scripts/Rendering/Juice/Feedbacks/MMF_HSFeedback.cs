using MoreMountains.Feedbacks;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.Feel
{
    // Base of every Hyperspeed feedback : drives the game's own systems (camera rig, post process, time, sound, FX), so
    // they add up with everything else instead of fighting it. Amount scales the whole feedback ; the event's intensity
    // (0 = weakest, 1 = full) is mapped through IntensityRemap.
    [System.Serializable]
    public abstract class MMF_HSFeedback : MMF_Feedback
    {
        [MMFInspectorGroup("Hyperspeed", true, 61)]
        [Tooltip("Multiplier on this whole feedback.")]
        public float Amount = 1f;
        [Tooltip("Strength at event intensity 0 (x) and 1 (y). (1,1) ignores the intensity.")]
        public Vector2 IntensityRemap = new Vector2(1f, 1f);

        // The event's context, frozen when this feedback starts (delays keep their own).
        protected JuiceContext Ctx;

        protected bool CanRun => Active && Application.isPlaying;

        public override void Play(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            Ctx = HSJuice.Current;
            base.Play(position, feedbacksIntensity);
        }

        protected float Strength(float pIntensity, Vector3 pPosition)
        {
            float lIntensity = ComputeIntensity(pIntensity, pPosition);
            return Amount * Mathf.LerpUnclamped(IntensityRemap.x, IntensityRemap.y, lIntensity);
        }

        protected Vector3 Where(Vector3 pPosition) => Ctx.position != Vector3.zero ? Ctx.position : pPosition;

        protected Vector3 Dir => Ctx.direction.sqrMagnitude > 0.01f ? Ctx.direction.normalized : Vector3.forward;

        protected Vector3 Normal => Ctx.normal.sqrMagnitude > 0.01f ? Ctx.normal.normalized : Vector3.up;
    }

    public enum HSColorSource { Fixed, ThemeAccent, ThemeRoad, ThemeRoadSide, ThemeDust, ThemeFog, Context }

    public static class HSColors
    {
        public static Color Pick(HSColorSource pSource, Color pFixed, JuiceContext pContext)
        {
            Theme lTheme = RaceContext.Theme;
            switch (pSource)
            {
                case HSColorSource.ThemeAccent: return lTheme != null ? lTheme.accent : pFixed;
                case HSColorSource.ThemeRoad: return lTheme != null ? lTheme.roadA : pFixed;
                case HSColorSource.ThemeRoadSide: return lTheme != null ? lTheme.roadSide : pFixed;
                case HSColorSource.ThemeDust: return lTheme != null ? Color.Lerp(lTheme.roadA, Color.white, 0.25f) : pFixed;
                case HSColorSource.ThemeFog: return lTheme != null ? lTheme.fog : pFixed;
                case HSColorSource.Context: return pContext.hasColor ? pContext.color : pFixed;
                default: return pFixed;
            }
        }
    }
}
