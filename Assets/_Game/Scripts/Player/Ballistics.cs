using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Shared air physics : the gravity profile (light going up, heavy coming down, floaty apex) and arc prediction
    // used by the controller, jump pads, the kicker assist, the air edge guard and the landing markers.
    public static class Ballistics
    {
        public const float G_RISE = 42f;
        public const float G_FALL = 58f;
        public const float APEX_SPEED = 5f;
        public const float APEX_SCALE = 0.6f;
        private const float PREDICT_STEP = 0.03f;

        public static float Gravity(float pVerticalSpeed)
        {
            float lGravity = pVerticalSpeed > 0f ? G_RISE : G_FALL;
            if (Mathf.Abs(pVerticalSpeed) < APEX_SPEED) lGravity *= APEX_SCALE;
            return lGravity;
        }

        public static float HeightAt(float pVerticalSpeed, float pTime)
        {
            const float DT = 0.01f;
            float lY = 0f;
            float lV = pVerticalSpeed;
            for (float lT = 0f; lT < pTime; lT += DT)
            {
                lV -= Gravity(lV) * DT;
                lY += lV * DT;
            }
            return lY;
        }

        // Vertical speed that covers pDistance at pHorizontalSpeed and arrives pDeltaHeight higher.
        public static float SolveVerticalSpeed(float pDistance, float pHorizontalSpeed, float pDeltaHeight)
        {
            float lTime = pDistance / Mathf.Max(1f, pHorizontalSpeed);
            float lLow = -20f;
            float lHigh = 120f;
            for (int i = 0; i < 26; i++)
            {
                float lMid = (lLow + lHigh) * 0.5f;
                if (HeightAt(lMid, lTime) < pDeltaHeight) lLow = lMid;
                else lHigh = lMid;
            }
            return lHigh;
        }

        // Steps a free-fall arc against the layout decks. pGravityScale lets glide / dive be predicted too.
        public static bool PredictLanding(LevelLayout pLayout, Vector3 pPosition, Vector3 pVelocity, float pHintS, float pGravityScale, float pMaxTime, bool pIgnoreMembrane, out DeckHit pHit, out float pTime, out float pS)
        {
            pHit = default(DeckHit);
            pTime = 0f;
            pS = pHintS;
            if (pLayout == null) return false;
            Vector3 lPosition = pPosition;
            Vector3 lVelocity = pVelocity;
            float lS = pHintS;
            for (float lT = 0f; lT < pMaxTime; lT += PREDICT_STEP)
            {
                lVelocity.y -= Gravity(lVelocity.y) * pGravityScale * PREDICT_STEP;
                lPosition += lVelocity * PREDICT_STEP;
                float lLateral;
                lS = pLayout.spine.Project(lPosition, lS, out lLateral);
                float lReach = 0.6f + Mathf.Max(0f, -lVelocity.y) * PREDICT_STEP;
                if (pLayout.SurfaceBelow(lPosition, lS, 0.4f, lReach, pIgnoreMembrane, out pHit))
                {
                    pTime = lT;
                    pS = lS;
                    return true;
                }
                if (lPosition.y < pLayout.FloorY(lS) - 30f) break;
            }
            return false;
        }
    }
}
