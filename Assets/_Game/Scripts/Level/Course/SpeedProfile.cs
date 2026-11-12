using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Everything tied to the speed of a stage. Geometry barely grows with it : a faster stage really scrolls faster.
    //
    // 2026-11-12, the crescendo : hyper and light are rare and late. No single kick may cross a tier : every kick is
    // capped by a ceiling that only a chain of pads, rings and ropes raises (KickCeiling). Hyper exists from 2-4 on
    // (hyperEnabled) ; light speed only from 3-4 on (lightSpeed > 0), reached by charging it with a long chain of pads.
    public class SpeedProfile
    {
        // Reference speed of everything else : run speed with no boost at all.
        public float cruise = 120f;
        // Top of the speed effects and the speedometer : not a limit, the only hard limit is SafetyMax.
        public float max = 400f;
        public float hyperEnter = 288f;
        public float hyperExit = 240f;
        // Top of the screen effects (Speed01 = 1) and the speed held in light form until the light highways take over.
        public float lightEnter = 396f;
        public float lightExit = 312f;
        public float strafeBase = 24f;
        public float strafeSlope = 0.38f;
        public float k = 1f;
        // Late stages : the runner breaks blocks on contact and tears the ground open.
        public bool worldBreaker;
        // Hyper speed exists on this stage.
        public bool hyperEnabled;
        // Light speed on this stage, in m/s (0 : none) ; pads it takes to charge it.
        public float lightSpeed;
        public int lightPads = 24;

        public const float SAFETY_MAX = 2400f;
        // Seconds between two links of a chain (pads, rings, rope exits).
        public const float CHAIN_WINDOW = 1.1f;

        public static SpeedProfile Current = new SpeedProfile();

        public bool LightEnabled => lightSpeed > 0f;

        // The hard limit : far above everything on ordinary stages, above light speed where it exists.
        public float SafetyMax => Mathf.Max(SAFETY_MAX, lightSpeed * 1.3f);

        // How fast kicks may take you with this chain : one pad from cruise lands about 1.7x cruise, a chain of five or
        // more opens hyper (2.4x) where it exists ; where it does not, kicks stop short of it.
        public float KickCeiling(int pChain)
        {
            float lRatio = 1.5f + 0.18f * Mathf.Max(0, pChain);
            if (!hyperEnabled) lRatio = Mathf.Min(lRatio, 2.2f);
            return cruise * lRatio;
        }

        public static SpeedProfile For(float pCruise, bool pWorldBreaker = false, bool pHyper = true, float pLightSpeed = 0f, int pLightPads = 24)
        {
            return new SpeedProfile
            {
                cruise = pCruise,
                max = pCruise * 3.8f,
                hyperEnter = pCruise * 2.4f,
                hyperExit = pCruise * 2f,
                lightEnter = pCruise * 3.3f,
                lightExit = pCruise * 2.6f,
                k = Mathf.Clamp(pCruise / 120f, 1f, 1.5f),
                worldBreaker = pWorldBreaker,
                hyperEnabled = pHyper,
                lightSpeed = pLightSpeed,
                lightPads = Mathf.Max(1, pLightPads)
            };
        }

        public float StrafeMax(float pSpeed) => strafeBase + strafeSlope * Mathf.Max(pSpeed, cruise * 0.6f);
    }
}
