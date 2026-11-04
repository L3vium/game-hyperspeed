using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Everything that scales with a stage's speed. World scale k = cruise / 90 : hills, pads, gaps and blocks grow with
    // the speed so a stage reads the same in seconds of play whatever its cruise speed.
    public class SpeedProfile
    {
        public float cruise = 90f;
        public float max = 200f;
        public float hyperEnter = 120f;
        public float hyperExit = 105f;
        public float lightEnter = 170f;
        public float lightExit = 150f;
        public float strafeBase = 20f;
        public float strafeSlope = 0.55f;
        public float gravityScale = 1f;
        public float k = 1f;
        // Late stages : the runner breaks blocks on contact and tears the ground open.
        public bool worldBreaker;

        public static SpeedProfile Current = new SpeedProfile();

        public static SpeedProfile For(float pCruise, bool pWorldBreaker = false)
        {
            float lK = pCruise / 90f;
            return new SpeedProfile
            {
                cruise = pCruise,
                max = pCruise * 1.85f,
                hyperEnter = pCruise * 1.28f,
                hyperExit = pCruise * 1.15f,
                lightEnter = Mathf.Max(170f, pCruise * 1.65f),
                lightExit = Mathf.Max(150f, pCruise * 1.45f),
                gravityScale = Mathf.Pow(lK, 0.65f),
                k = lK,
                worldBreaker = pWorldBreaker
            };
        }

        public float StrafeMax(float pSpeed) => strafeBase + strafeSlope * Mathf.Max(pSpeed, cruise * 0.6f);
    }
}
