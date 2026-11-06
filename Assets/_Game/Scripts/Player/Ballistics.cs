using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Shared air physics : heavy, no float at the top of a jump.
    public static class Ballistics
    {
        public const float G_RISE = 115f;
        public const float G_FALL = 150f;

        public static float Gravity(float pVerticalSpeed) => pVerticalSpeed > 0f ? G_RISE : G_FALL;
    }
}
