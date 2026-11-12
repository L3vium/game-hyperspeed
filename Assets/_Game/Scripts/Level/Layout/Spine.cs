using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public struct SpineSample
    {
        public Vector3 position;
        public Vector3 forward;
        public Vector3 up;
        public float s;
        // Gravity follows the track here (loops, twists, inverted runs).
        public bool localGravity;

        public Vector3 Forward => forward;
        public Vector3 Up => up;
        public Vector3 Right => Vector3.Cross(up, forward).normalized;
        public float Yaw => Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }

    // Racing line of a level : a curve in 3D carrying a full frame, so the track can bank, turn upside down and loop.
    // Progress, rivals and respawn are expressed on it as (s, lateral). Searches stay around a hint, so two legs of the
    // track passing near each other can never mislead the progress.
    public class Spine
    {
        public const float STEP = 2f;

        public readonly List<SpineSample> samples = new List<SpineSample>();

        public int Count => samples.Count;
        public float StartS => samples[0].s;
        public float EndS => samples[samples.Count - 1].s;

        public int IndexOf(float pS) => Mathf.Clamp(Mathf.RoundToInt((pS - StartS) / STEP), 0, samples.Count - 1);

        public int FloorIndex(float pS) => Mathf.Clamp(Mathf.FloorToInt((pS - StartS) / STEP), 0, samples.Count - 1);

        public float SOf(int pIndex) => StartS + pIndex * STEP;

        public SpineSample Evaluate(float pS)
        {
            float lF = (pS - StartS) / STEP;
            int lA = Mathf.Clamp(Mathf.FloorToInt(lF), 0, samples.Count - 1);
            int lB = Mathf.Min(lA + 1, samples.Count - 1);
            float lT = Mathf.Clamp01(lF - lA);
            SpineSample lSa = samples[lA];
            SpineSample lSb = samples[lB];
            SpineSample lResult = lSa;
            lResult.position = Vector3.LerpUnclamped(lSa.position, lSb.position, lT);
            lResult.forward = Vector3.Slerp(lSa.forward, lSb.forward, lT).normalized;
            lResult.up = Vector3.Slerp(lSa.up, lSb.up, lT).normalized;
            lResult.s = pS;
            // Past both ends the line keeps going straight.
            if (pS < StartS) lResult.position = lSa.position + lSa.forward * (pS - StartS);
            else if (pS > EndS)
            {
                SpineSample lLast = samples[samples.Count - 1];
                lResult.position = lLast.position + lLast.forward * (pS - EndS);
            }
            return lResult;
        }

        public Vector3 Forward(float pS) => Evaluate(pS).forward;

        public Vector3 Right(float pS) => Evaluate(pS).Right;

        public Vector3 Up(float pS) => Evaluate(pS).up;

        // World point at (s, lateral, height above the line).
        public Vector3 Point(float pS, float pLateral, float pHeight = 0f)
        {
            SpineSample lSample = Evaluate(pS);
            return lSample.position + lSample.Right * pLateral + lSample.up * pHeight;
        }

        public float Project(Vector3 pPosition, float pHintS, out float pLateral) => Search(pPosition, pHintS, 20, 120, out pLateral);

        // Narrow search for the movement substeps.
        public float ProjectNear(Vector3 pPosition, float pHintS, out float pLateral) => Search(pPosition, pHintS, 4, 10, out pLateral);

        // Same projection with a global coarse search (spawn, teleports).
        public float ProjectGlobal(Vector3 pPosition, out float pLateral)
        {
            int lBest = 0;
            float lBestSqr = float.MaxValue;
            for (int i = 0; i < samples.Count; i += 4)
            {
                float lSqr = (pPosition - samples[i].position).sqrMagnitude;
                if (lSqr < lBestSqr)
                {
                    lBestSqr = lSqr;
                    lBest = i;
                }
            }
            return Project(pPosition, SOf(lBest), out pLateral);
        }

        private float Search(Vector3 pPosition, float pHintS, int pBack, int pAhead, out float pLateral)
        {
            int lHint = IndexOf(pHintS);
            int lStart = Mathf.Max(0, lHint - pBack);
            int lEnd = Mathf.Min(samples.Count - 1, lHint + pAhead);
            int lBest = lHint;
            float lBestSqr = float.MaxValue;
            for (int i = lStart; i <= lEnd; i++)
            {
                // Out of the loops, height counts for little : a runner on a deck 500 m above the line, or on a slab
                // far below it, is where it is along the line, not stuck at the top of the next drop (2026-11-12).
                Vector3 lDelta = pPosition - samples[i].position;
                float lSqr = samples[i].localGravity ? lDelta.sqrMagnitude : lDelta.x * lDelta.x + lDelta.z * lDelta.z + lDelta.y * lDelta.y * 0.04f;
                if (lSqr < lBestSqr)
                {
                    lBestSqr = lSqr;
                    lBest = i;
                }
            }
            SpineSample lSample = samples[lBest];
            Vector3 lOffset = pPosition - lSample.position;
            float lAlong = Mathf.Clamp(Vector3.Dot(lOffset, lSample.forward), -STEP, STEP);
            float lS = lSample.s + lAlong;
            SpineSample lAt = Evaluate(lS);
            pLateral = Vector3.Dot(pPosition - lAt.position, lAt.Right);
            return lS;
        }
    }
}
