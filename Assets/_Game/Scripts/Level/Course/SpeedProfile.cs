using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Everything tied to the speed of a stage. Geometry barely grows with it : a faster stage really scrolls faster.
    public class SpeedProfile
    {
        // Reference speed of everything else : run speed with no boost at all.
        public float cruise = 120f;
        // Top of the speed effects and the speedometer : not a limit, the only hard limit is SAFETY_MAX.
        public float max = 400f;
        public float hyperEnter = 230f;
        public float hyperExit = 190f;
        public float lightEnter = 360f;
        public float lightExit = 260f;
        public float strafeBase = 24f;
        public float strafeSlope = 0.38f;
        public float k = 1f;
        // Late stages : the runner breaks blocks on contact and tears the ground open.
        public bool worldBreaker;

        public const float SAFETY_MAX = 2400f;

        public static SpeedProfile Current = new SpeedProfile();

        public static SpeedProfile For(float pCruise, bool pWorldBreaker = false)
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
                worldBreaker = pWorldBreaker
            };
        }

        public float StrafeMax(float pSpeed) => strafeBase + strafeSlope * Mathf.Max(pSpeed, cruise * 0.6f);
    }
}
