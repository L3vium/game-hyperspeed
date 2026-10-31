using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public struct TrackSample
    {
        public Vector3 position;
        public Quaternion rotation;
        public float distance;
        public float width;
        public bool solid;

        public Vector3 Forward => rotation * Vector3.forward;
        public Vector3 Up => rotation * Vector3.up;
        public Vector3 Right => rotation * Vector3.right;
    }

    // Center line of the generated track : progress, respawn frames, rival and prop placement.
    public class TrackPath
    {
        public readonly List<TrackSample> samples = new List<TrackSample>();
        public readonly List<float> checkpoints = new List<float>();
        public readonly List<KeyValuePair<float, string>> hints = new List<KeyValuePair<float, string>>();
        public float finishDistance;

        public float Length => samples.Count > 0 ? samples[samples.Count - 1].distance : 0f;

        public void Add(TrackSample pSample)
        {
            if (samples.Count > 0 && pSample.distance - samples[samples.Count - 1].distance < 0.01f)
                samples[samples.Count - 1] = pSample;
            else
                samples.Add(pSample);
        }

        public TrackSample Evaluate(float pDistance)
        {
            if (samples.Count == 0) return default(TrackSample);
            if (pDistance <= 0f) return samples[0];
            if (pDistance >= Length) return samples[samples.Count - 1];

            int lLow = 0;
            int lHigh = samples.Count - 1;
            while (lHigh - lLow > 1)
            {
                int lMid = (lLow + lHigh) / 2;
                if (samples[lMid].distance < pDistance) lLow = lMid;
                else lHigh = lMid;
            }
            TrackSample lA = samples[lLow];
            TrackSample lB = samples[lHigh];
            float lT = Mathf.InverseLerp(lA.distance, lB.distance, pDistance);
            TrackSample lResult = lA;
            lResult.position = Vector3.Lerp(lA.position, lB.position, lT);
            lResult.rotation = Quaternion.Slerp(lA.rotation, lB.rotation, lT);
            lResult.distance = pDistance;
            lResult.width = Mathf.Lerp(lA.width, lB.width, lT);
            lResult.solid = lA.solid && lB.solid;
            return lResult;
        }

        // Local search around a hint index (the player moves continuously along the track).
        public int FindNearest(Vector3 pPosition, int pHint, int pWindow = 60)
        {
            if (samples.Count == 0) return 0;
            int lStart = Mathf.Max(0, pHint - pWindow);
            int lEnd = Mathf.Min(samples.Count - 1, pHint + pWindow * 2);
            int lBest = pHint;
            float lBestDistance = float.MaxValue;
            for (int i = lStart; i <= lEnd; i++)
            {
                float lSqr = (samples[i].position - pPosition).sqrMagnitude;
                if (lSqr < lBestDistance)
                {
                    lBestDistance = lSqr;
                    lBest = i;
                }
            }
            return lBest;
        }

        public int FindNearestGlobal(Vector3 pPosition)
        {
            int lBest = 0;
            float lBestDistance = float.MaxValue;
            for (int i = 0; i < samples.Count; i += 4)
            {
                float lSqr = (samples[i].position - pPosition).sqrMagnitude;
                if (lSqr < lBestDistance)
                {
                    lBestDistance = lSqr;
                    lBest = i;
                }
            }
            return FindNearest(pPosition, lBest, 8);
        }

        // Projected distance of a point along the path near a sample index.
        public float ProjectDistance(Vector3 pPosition, int pIndex)
        {
            TrackSample lSample = samples[pIndex];
            float lAlong = Vector3.Dot(pPosition - lSample.position, lSample.Forward);
            return Mathf.Clamp(lSample.distance + lAlong, 0f, Length);
        }

        public float LastCheckpointBefore(float pDistance)
        {
            float lResult = 0f;
            foreach (float lCheckpoint in checkpoints)
                if (lCheckpoint <= pDistance) lResult = lCheckpoint;
            return lResult;
        }
    }
}
