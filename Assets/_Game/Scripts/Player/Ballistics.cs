using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Shared air physics : the gravity profile (light going up, heavy coming down, floaty apex), scaled by stage so the
    // flights last the same time at any speed, and arc prediction against the terrain.
    public static class Ballistics
    {
        public const float G_RISE = 42f;
        public const float G_FALL = 58f;
        public const float APEX_SPEED = 5f;
        public const float APEX_SCALE = 0.6f;
        private const float PREDICT_STEP = 0.05f;

        public static float Scale = 1f;

        public static float Gravity(float pVerticalSpeed)
        {
            float lGravity = (pVerticalSpeed > 0f ? G_RISE : G_FALL) * Scale;
            if (Mathf.Abs(pVerticalSpeed) < APEX_SPEED) lGravity *= APEX_SCALE;
            return lGravity;
        }

        public static float HeightAt(float pVerticalSpeed, float pTime)
        {
            const float DT = 0.02f;
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
            float lLow = -40f;
            float lHigh = 800f;
            for (int i = 0; i < 22; i++)
            {
                float lMid = (lLow + lHigh) * 0.5f;
                if (HeightAt(lMid, lTime) < pDeltaHeight) lLow = lMid;
                else lHigh = lMid;
            }
            return lHigh;
        }

        // Steps the arc in route coordinates (the flight follows the route) against the terrain.
        public static bool PredictLanding(LevelLayout pLayout, float pS, float pLateral, float pY, float pAlong, float pLateralSpeed, float pVertical, float pGravityScale, float pMaxTime, out Vector3 pPoint, out float pTime)
        {
            pPoint = Vector3.zero;
            pTime = 0f;
            float lY = pY;
            float lV = pVertical;
            float lS = pS;
            float lL = pLateral;
            for (float lT = 0f; lT < pMaxTime; lT += PREDICT_STEP)
            {
                lV -= Gravity(lV) * pGravityScale * PREDICT_STEP;
                lY += lV * PREDICT_STEP;
                lS += pAlong * PREDICT_STEP;
                lL += pLateralSpeed * PREDICT_STEP;
                if (!pLayout.field.OverGround(lS, lL)) continue;
                float lGround = pLayout.Ground(lS, lL);
                if (lY <= lGround)
                {
                    pTime = lT;
                    pPoint = pLayout.spine.Point(lS, lL, lGround);
                    return true;
                }
            }
            return false;
        }
    }
}
