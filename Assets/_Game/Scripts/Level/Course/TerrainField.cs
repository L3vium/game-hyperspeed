using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public static class Layers
    {
        public const int DECK = 8;
        public const int PROP = 10;
        public const int DEBRIS = 11;

        public const int GROUND = (1 << 0) | (1 << DECK) | (1 << PROP);
    }

    // A bump (hill, mountain to run over) or a trough (funnel toward a rope) stamped on the terrain.
    public struct TerrainStamp
    {
        public float s;
        public float lateral;
        public float radiusS;
        public float radiusL;
        public float height;
        // Funnel : raises everything but the axis, inside an s window.
        public bool funnel;
    }

    // The ground of a level : one height for each (s, lateral), so platforms can never stack. Heights are analytic :
    // the mesh, the prediction, the camera clamp and the respawn all read the same function.
    public class TerrainField
    {
        private const float BUCKET = 128f;

        public readonly Spine spine;
        public float[] halfWidth;
        public float[] offset;
        public float[] hill;
        public float[] road;
        public float[] sideFall;
        public readonly List<float> creases = new List<float>();

        public float bermWidth = 60f;
        public float bermHeight = 22f;
        public float skirtWidth = 30f;
        public float skirtDrop = 900f;
        public float hillWave = 320f;

        private readonly List<TerrainStamp> _stamps = new List<TerrainStamp>();
        private List<int>[] _buckets;
        private float[] _y;
        private float _noiseX;
        private float _noiseY;

        public TerrainField(Spine pSpine, int pSeed)
        {
            spine = pSpine;
            System.Random lRandom = new System.Random(pSeed);
            _noiseX = (float)lRandom.NextDouble() * 1000f;
            _noiseY = (float)lRandom.NextDouble() * 1000f;
        }

        public void AddStamp(TerrainStamp pStamp) => _stamps.Add(pStamp);

        public void Seal()
        {
            _y = new float[spine.Count];
            for (int i = 0; i < spine.Count; i++) _y[i] = spine.samples[i].position.y;
            int lCount = Mathf.CeilToInt((spine.EndS - spine.StartS) / BUCKET) + 1;
            _buckets = new List<int>[lCount];
            for (int i = 0; i < lCount; i++) _buckets[i] = new List<int>(2);
            for (int i = 0; i < _stamps.Count; i++)
            {
                TerrainStamp lStamp = _stamps[i];
                int lFrom = Mathf.Clamp(Mathf.FloorToInt((lStamp.s - lStamp.radiusS - spine.StartS) / BUCKET), 0, lCount - 1);
                int lTo = Mathf.Clamp(Mathf.FloorToInt((lStamp.s + lStamp.radiusS - spine.StartS) / BUCKET), 0, lCount - 1);
                for (int j = lFrom; j <= lTo; j++) _buckets[j].Add(i);
            }
        }

        private float BaseY(float pS)
        {
            float lF = (pS - spine.StartS) / Spine.STEP;
            int lA = Mathf.Clamp(Mathf.FloorToInt(lF), 0, _y.Length - 1);
            int lB = Mathf.Min(lA + 1, _y.Length - 1);
            return Mathf.LerpUnclamped(_y[lA], _y[lB], Mathf.Clamp01(lF - lA));
        }

        public float HalfWidth(float pS) => halfWidth[spine.IndexOf(pS)];

        public float Offset(float pS) => offset[spine.IndexOf(pS)];

        public float TotalHalfWidth(float pS) => HalfWidth(pS) + bermWidth + skirtWidth;

        // Lateral room to play in, relative to the spine.
        public bool Inside(float pS, float pLateral) => Mathf.Abs(pLateral - Offset(pS)) <= HalfWidth(pS) + bermWidth * 0.5f;

        public bool OverGround(float pS, float pLateral) => pS > spine.StartS && pS < spine.EndS && Mathf.Abs(pLateral - Offset(pS)) <= HalfWidth(pS) + bermWidth + skirtWidth * 0.2f;

        public float Height(float pS, float pLateral)
        {
            int lI = spine.IndexOf(pS);
            float lX = Mathf.Abs(pLateral - offset[lI]) - halfWidth[lI];
            float lHeight = BaseY(pS);

            float lHill = hill[lI];
            float lInside = lX <= 0f ? 1f : Mathf.Clamp01(1f - lX / bermWidth);
            if (lHill > 0.01f) lHeight += lHill * lInside * HillNoise(pS, pLateral);

            int lBucket = Mathf.Clamp(Mathf.FloorToInt((pS - spine.StartS) / BUCKET), 0, _buckets.Length - 1);
            List<int> lList = _buckets[lBucket];
            for (int i = 0; i < lList.Count; i++)
            {
                TerrainStamp lStamp = _stamps[lList[i]];
                float lDs = (pS - lStamp.s) / lStamp.radiusS;
                if (lDs <= -1f || lDs >= 1f) continue;
                float lDl = (pLateral - lStamp.lateral) / lStamp.radiusL;
                if (lStamp.funnel)
                {
                    float lWindow = 1f - lDs * lDs;
                    lHeight += lStamp.height * lWindow * lWindow * Mathf.Min(1f, lDl * lDl);
                    continue;
                }
                if (lDl <= -1f || lDl >= 1f) continue;
                float lR = lDs * lDs + lDl * lDl;
                if (lR >= 1f) continue;
                float lFall = 1f - lR;
                lHeight += lStamp.height * lFall * lFall * (3f - 2f * lFall);
            }

            if (lX > 0f)
            {
                float lFade = sideFall != null ? sideFall[lI] : 1f;
                if (lX < bermWidth)
                {
                    float lT = lX / bermWidth;
                    lHeight += bermHeight * lT * lT * (3f - 2f * lT) * lFade;
                }
                else
                {
                    float lT = Mathf.Min(1f, (lX - bermWidth) / skirtWidth);
                    lHeight += bermHeight * lFade - skirtDrop * lT * lT;
                }
            }
            return lHeight;
        }

        private float HillNoise(float pS, float pLateral)
        {
            float lA = Mathf.PerlinNoise(pS / hillWave + _noiseX, pLateral / (hillWave * 0.7f) + _noiseY);
            float lB = Mathf.PerlinNoise(pS / (hillWave * 0.43f) + _noiseY, pLateral / (hillWave * 0.33f) + _noiseX);
            return (lA * 0.8f + lB * 0.2f) * 2f - 1f;
        }

        // World position of (s, lateral) on the ground.
        public Vector3 Point(float pS, float pLateral) => spine.Point(pS, pLateral, Height(pS, pLateral));

        public Vector3 Normal(float pS, float pLateral, float pStep)
        {
            SpineSample lSample = spine.Evaluate(pS);
            float lDs = (Height(pS + pStep, pLateral) - Height(pS - pStep, pLateral)) / (2f * pStep);
            float lDl = (Height(pS, pLateral + pStep) - Height(pS, pLateral - pStep)) / (2f * pStep);
            return (Vector3.up - lSample.Forward * lDs - lSample.Right * lDl).normalized;
        }

        // Downward curvature along the route around s (1/m), positive on a crest.
        public float Convexity(float pS, float pLateral, float pStep)
        {
            float lMid = Height(pS, pLateral);
            return (2f * lMid - Height(pS - pStep, pLateral) - Height(pS + pStep, pLateral)) / (pStep * pStep);
        }
    }
}
