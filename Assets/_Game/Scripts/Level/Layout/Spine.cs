using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public struct SpineSample
    {
        public Vector3 position;
        public float yaw;
        public float s;

        public Vector3 Forward => new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
        public Vector3 Right => new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad), 0f, -Mathf.Sin(yaw * Mathf.Deg2Rad));
    }

    // Racing line of a level : a plan-view curve (yaw-only frames, s = plan arc length) plus a height profile.
    // Progress, rivals, respawn and every deck are expressed relative to it as (s, lateral, height).
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
            lResult.yaw = Mathf.LerpAngle(lSa.yaw, lSb.yaw, lT);
            lResult.s = pS;
            // Past both ends the line keeps going straight.
            if (pS < StartS) lResult.position = lSa.position + lSa.Forward * (pS - StartS);
            else if (pS > EndS) lResult.position = samples[samples.Count - 1].position + samples[samples.Count - 1].Forward * (pS - EndS);
            return lResult;
        }

        public Vector3 Forward(float pS) => Evaluate(pS).Forward;

        public Vector3 Right(float pS) => Evaluate(pS).Right;

        public float Height(float pS) => Evaluate(pS).position.y;

        // World point at (s, lateral offset, absolute height).
        public Vector3 Point(float pS, float pLateral, float pHeight)
        {
            SpineSample lSample = Evaluate(pS);
            Vector3 lPoint = lSample.position + lSample.Right * pLateral;
            lPoint.y = pHeight;
            return lPoint;
        }

        // Plan-view projection : heights are ignored, so stacked decks can never mislead the progress.
        public float Project(Vector3 pPosition, float pHintS, out float pLateral)
        {
            int lHint = IndexOf(pHintS);
            int lStart = Mathf.Max(0, lHint - 20);
            int lEnd = Mathf.Min(samples.Count - 1, lHint + 120);
            int lBest = lHint;
            float lBestSqr = float.MaxValue;
            for (int i = lStart; i <= lEnd; i++)
            {
                float lDx = pPosition.x - samples[i].position.x;
                float lDz = pPosition.z - samples[i].position.z;
                float lSqr = lDx * lDx + lDz * lDz;
                if (lSqr < lBestSqr)
                {
                    lBestSqr = lSqr;
                    lBest = i;
                }
            }
            return ProjectFrom(pPosition, lBest, out pLateral);
        }

        // Narrow search for the movement substeps.
        public float ProjectNear(Vector3 pPosition, float pHintS, out float pLateral)
        {
            int lHint = IndexOf(pHintS);
            int lStart = Mathf.Max(0, lHint - 4);
            int lEnd = Mathf.Min(samples.Count - 1, lHint + 8);
            int lBest = lHint;
            float lBestSqr = float.MaxValue;
            for (int i = lStart; i <= lEnd; i++)
            {
                float lDx = pPosition.x - samples[i].position.x;
                float lDz = pPosition.z - samples[i].position.z;
                float lSqr = lDx * lDx + lDz * lDz;
                if (lSqr < lBestSqr)
                {
                    lBestSqr = lSqr;
                    lBest = i;
                }
            }
            return ProjectFrom(pPosition, lBest, out pLateral);
        }

        // Same projection with a global coarse search (spawn, teleports).
        public float ProjectGlobal(Vector3 pPosition, out float pLateral)
        {
            int lBest = 0;
            float lBestSqr = float.MaxValue;
            for (int i = 0; i < samples.Count; i += 4)
            {
                float lDx = pPosition.x - samples[i].position.x;
                float lDz = pPosition.z - samples[i].position.z;
                float lSqr = lDx * lDx + lDz * lDz;
                if (lSqr < lBestSqr)
                {
                    lBestSqr = lSqr;
                    lBest = i;
                }
            }
            return Project(pPosition, SOf(lBest), out pLateral);
        }

        private float ProjectFrom(Vector3 pPosition, int pIndex, out float pLateral)
        {
            SpineSample lSample = samples[pIndex];
            Vector3 lDelta = pPosition - lSample.position;
            lDelta.y = 0f;
            float lAlong = Mathf.Clamp(Vector3.Dot(lDelta, lSample.Forward), -STEP, STEP);
            float lS = lSample.s + lAlong;
            SpineSample lAt = Evaluate(lS);
            Vector3 lFlat = pPosition - lAt.position;
            lFlat.y = 0f;
            pLateral = Vector3.Dot(lFlat, lAt.Right);
            return lS;
        }
    }
}
