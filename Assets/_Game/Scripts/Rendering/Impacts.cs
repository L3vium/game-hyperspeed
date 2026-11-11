using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum ImpactReason { LightSpeed, BlockBroken, Finish, Smash, Boost, Dash, Big }

    // The only door to an impact frame. Each reason has its own cooldown (one kind never blocks another), plus a tiny
    // gap so two never stack in the same instant. Nothing on the title or the menus : no race, no frame.
    public static class Impacts
    {
        private const float GAP = 0.1f;

        private static readonly float[] COOLDOWN = { 0f, 1.5f, 0f, 0.12f, 0.35f, 0.3f, 0.5f };
        private static readonly float[] _last = new float[COOLDOWN.Length];
        private static float _any = -100f;

        public static int Count { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (int i = 0; i < _last.Length; i++) _last[i] = -100f;
            _any = -100f;
            Count = 0;
        }

        public static void Request(ImpactReason pReason) => Request(pReason, null);

        public static void Request(ImpactReason pReason, Vector3? pWorldPoint) =>
            Request(pReason, pWorldPoint, pReason == ImpactReason.BlockBroken || pReason == ImpactReason.Smash ? 0.8f : 1f, 0);

        // pInkFrames 0 : the default for the reason.
        public static void Request(ImpactReason pReason, Vector3? pWorldPoint, float pStrength, int pInkFrames)
        {
            if (!RaceContext.Running && pReason != ImpactReason.Finish) return;
            float lNow = Time.unscaledTime;
            int lIndex = (int)pReason;
            if (lNow - _last[lIndex] < COOLDOWN[lIndex]) return;
            if (lNow - _any < GAP && pReason != ImpactReason.Finish && pReason != ImpactReason.LightSpeed) return;
            _last[lIndex] = lNow;
            _any = lNow;
            Count++;
            Juice.ImpactFrame(pStrength, pWorldPoint, pInkFrames);
            if (Autopilot.Enabled) Debug.Log("[HS-FEEL] impact frame " + pReason + " at t=" + Time.time.ToString("F1") + " total=" + Count);
        }
    }
}
