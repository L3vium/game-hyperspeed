using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum ImpactReason { LightSpeed, BlockBroken, Finish }

    // The only door to an impact frame : they stay rare, so they stay huge. Entering light speed and the finish always
    // get one, a broken block only if none was shown lately. Nothing on the title or the menus : no race, no frame.
    public static class Impacts
    {
        private const float BLOCK_COOLDOWN = 6f;

        private static float _last = -100f;

        public static int Count { get; private set; }

        public static void Request(ImpactReason pReason)
        {
            if (!RaceContext.Running && pReason != ImpactReason.Finish) return;
            float lNow = Time.unscaledTime;
            if (pReason == ImpactReason.BlockBroken && lNow - _last < BLOCK_COOLDOWN) return;
            _last = lNow;
            Count++;
            Juice.ImpactFrame(pReason == ImpactReason.BlockBroken ? 0.8f : 1f);
            Debug.Log("[HS-FEEL] impact frame " + pReason + " at t=" + Time.time.ToString("F1") + " total=" + Count);
        }
    }
}
