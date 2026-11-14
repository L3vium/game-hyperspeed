using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Everything tied to the speed of a stage.
    //
    // 2026-11-14 : the movement of build 2398ecf5 (hyper at 1.9x cruise, light at 3.3x, on every stage, kicks that fade)
    // with the crescendo asked for : the first stage runs at about 110 m/s, the last one ten times faster, and the stage
    // grows with it (scale = cruise / first cruise : 1 on 1-1, 10 on 3-6). No tier gating, no kick ceiling.
    public class SpeedProfile
    {
        // Cruise of the very first stage : the reference of the scale.
        public const float FIRST_CRUISE = 110f;

        // Reference speed of everything else : run speed with no boost at all.
        public float cruise = FIRST_CRUISE;
        // Top of the speed effects and the speedometer : not a limit, the only hard limit is SafetyMax.
        public float max = 400f;
        public float hyperEnter = 209f;
        public float hyperExit = 176f;
        public float lightEnter = 363f;
        public float lightExit = 264f;
        public float strafeBase = 24f;
        public float strafeSlope = 0.38f;
        public float k = 1f;
        // How much bigger and faster this stage is than the first one (1 to 10).
        public float scale = 1f;
        // 0 on the first stage, 1 on the last : pads are softer early (calm first stages), full late (much faster).
        public float difficulty;
        public float PadPower => Mathf.Lerp(0.5f, 1.05f, difficulty);
        public float PadFloor => Mathf.Lerp(0.3f, 0.85f, difficulty);
        // Late stages : the runner breaks blocks on contact and tears the ground open.
        public bool worldBreaker;
        // Kept for the systems that ask : hyper and light exist on every stage.
        public bool hyperEnabled = true;
        public float lightSpeed;

        public const float SAFETY_MAX = 2400f;

        public static SpeedProfile Current = new SpeedProfile();

        public bool LightEnabled => true;

        // The hard limit : far above anything a run reaches, ten times further on the last stage.
        public float SafetyMax => Mathf.Max(SAFETY_MAX, cruise * 9f);

        public static SpeedProfile For(float pCruise, bool pWorldBreaker = false, bool pHyper = true, float pLightSpeed = 0f, int pLightPads = 24)
        {
            return new SpeedProfile
            {
                cruise = pCruise,
                max = pCruise * 3.8f,
                hyperEnter = pCruise * 1.9f,
                hyperExit = pCruise * 1.6f,
                lightEnter = pCruise * 3.3f,
                lightExit = pCruise * 2.4f,
                k = Mathf.Clamp(pCruise / 120f, 1f, 1.5f),
                scale = Mathf.Max(1f, pCruise / FIRST_CRUISE),
                worldBreaker = pWorldBreaker,
                hyperEnabled = true,
                lightSpeed = 0f
            };
        }

        // Sideways speed : grows with the run, and with the stage so a wide late stage can still be crossed.
        public float StrafeMax(float pSpeed) => (strafeBase + strafeSlope * Mathf.Max(pSpeed, cruise * 0.6f)) * 1.3f;
    }
}
