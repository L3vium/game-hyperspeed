using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum ImpactReason { LightSpeed, BlockBroken, Finish, Smash }

    // The only door to an impact frame. Entering light speed and the finish always get one ; every block you dash
    // through gets its own, set on the block in the world. Nothing on the title or the menus : no race, no frame.
    public static class Impacts
    {
        private const float BLOCK_COOLDOWN = 6f;
        private const float SMASH_COOLDOWN = 0.25f;

        private static float _last = -100f;

        public static int Count { get; private set; }

        public static void Request(ImpactReason pReason) => Request(pReason, null);

        public static void Request(ImpactReason pReason, Vector3? pWorldPoint)
        {
            if (!RaceContext.Running && pReason != ImpactReason.Finish) return;
            float lNow = Time.unscaledTime;
            if (pReason == ImpactReason.BlockBroken && lNow - _last < BLOCK_COOLDOWN) return;
            if (pReason == ImpactReason.Smash && lNow - _last < SMASH_COOLDOWN) return;
            _last = lNow;
            Count++;
            Juice.ImpactFrame(pReason == ImpactReason.BlockBroken || pReason == ImpactReason.Smash ? 0.8f : 1f, pWorldPoint);
            Debug.Log("[HS-FEEL] impact frame " + pReason + " at t=" + Time.time.ToString("F1") + " total=" + Count);
        }
    }
}
