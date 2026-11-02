using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Lip : you only fall off by pushing against it on purpose. Wall : never. Open : free exit. Joined : another deck continues.
    public enum EdgeKind : byte { Lip, Wall, Open, Joined }

    public enum DeckSurface : byte { Solid, Membrane, Crumble, Lane }

    public enum DeckStyle : byte { Rock, Slab, Road, Track, Metal, Crystal, Membrane }

    // Cross-section : flat, V trough funnelling onto a rail, or half V along one edge.
    public enum DeckProfile : byte { Flat, Trough, GutterL, GutterR }

    // One platform : offsets over a range of spine samples. Gameplay reads it analytically (never the meshes).
    public class Deck
    {
        public const float TROUGH_WIDTH = 8f;
        public const float TROUGH_DEPTH = 1.2f;

        public int id;
        public DeckStyle style;
        public DeckSurface surface;
        public bool route;
        public int i0;
        public int i1;
        public float[] x;
        public float[] h;
        public float[] w;
        public float[] bank;
        public EdgeKind[] edgeL;
        public EdgeKind[] edgeR;
        public DeckProfile[] profile;
        public float[] troughT;
        public float underDepth = 12f;
        public float adhesion = 30f;
        public bool startCap = true;
        public bool endCap = true;
        public Color tint = Color.white;

        public Vector3[] center;
        public Vector3[] fwd;
        public Vector3[] right;
        public Vector3[] up;
        public float[] convexity;
        public float[] slope;

        private Spine _spine;

        public int Count => i1 - i0 + 1;

        public Deck(int pId, int pI0, int pI1)
        {
            id = pId;
            i0 = pI0;
            i1 = pI1;
            Allocate(pI1 - pI0 + 1);
        }

        private void Allocate(int pCount)
        {
            x = new float[pCount];
            h = new float[pCount];
            w = new float[pCount];
            bank = new float[pCount];
            edgeL = new EdgeKind[pCount];
            edgeR = new EdgeKind[pCount];
            profile = new DeckProfile[pCount];
            troughT = new float[pCount];
        }

        // Grows the deck forward (used by the builder to extend the main route without seams).
        public void ExtendTo(int pI1)
        {
            if (pI1 <= i1) return;
            int lOld = Count;
            int lNew = pI1 - i0 + 1;
            System.Array.Resize(ref x, lNew);
            System.Array.Resize(ref h, lNew);
            System.Array.Resize(ref w, lNew);
            System.Array.Resize(ref bank, lNew);
            System.Array.Resize(ref edgeL, lNew);
            System.Array.Resize(ref edgeR, lNew);
            System.Array.Resize(ref profile, lNew);
            System.Array.Resize(ref troughT, lNew);
            for (int k = lOld; k < lNew; k++)
            {
                x[k] = x[lOld - 1];
                h[k] = h[lOld - 1];
                w[k] = w[lOld - 1];
                edgeL[k] = edgeL[lOld - 1];
                edgeR[k] = edgeR[lOld - 1];
            }
            i1 = pI1;
        }

        public bool Contains(int pSpineIndex) => pSpineIndex >= i0 && pSpineIndex <= i1;

        public bool ContainsS(float pS) => _spine != null && pS >= _spine.SOf(i0) - 0.5f && pS <= _spine.SOf(i1) + 0.5f;

        public float StartS => _spine.SOf(i0);

        public float EndS => _spine.SOf(i1);

        public void Finalize(Spine pSpine)
        {
            _spine = pSpine;
            int lCount = Count;
            center = new Vector3[lCount];
            fwd = new Vector3[lCount];
            right = new Vector3[lCount];
            up = new Vector3[lCount];
            convexity = new float[lCount];
            slope = new float[lCount];
            for (int k = 0; k < lCount; k++)
            {
                SpineSample lSample = pSpine.samples[i0 + k];
                Vector3 lPoint = lSample.position + lSample.Right * x[k];
                lPoint.y = h[k];
                center[k] = lPoint;
            }
            for (int k = 0; k < lCount; k++)
            {
                Vector3 lPrev = center[Mathf.Max(0, k - 1)];
                Vector3 lNext = center[Mathf.Min(lCount - 1, k + 1)];
                Vector3 lForward = lNext - lPrev;
                if (lForward.sqrMagnitude < 0.0001f) lForward = pSpine.samples[i0 + k].Forward;
                lForward.Normalize();
                Vector3 lFlatRight = Vector3.Cross(Vector3.up, lForward).normalized;
                Vector3 lUp = Vector3.Cross(lForward, lFlatRight).normalized;
                Quaternion lBank = Quaternion.AngleAxis(-bank[k], lForward);
                fwd[k] = lForward;
                right[k] = lBank * lFlatRight;
                up[k] = lBank * lUp;
                slope[k] = Mathf.Asin(Mathf.Clamp(lForward.y, -1f, 1f));
            }
            // Convexity in 1/m (positive on crests), from the analytic pitch : collider facets would read as infinite curvature.
            for (int k = 0; k < lCount; k++)
            {
                int lA = Mathf.Max(0, k - 1);
                int lB = Mathf.Min(lCount - 1, k + 1);
                float lLength = Vector3.Distance(center[lA], center[lB]);
                convexity[k] = lLength > 0.01f ? (slope[lA] - slope[lB]) / lLength : 0f;
            }
            float[] lSmooth = new float[lCount];
            for (int k = 0; k < lCount; k++)
                lSmooth[k] = (convexity[Mathf.Max(0, k - 1)] + convexity[k] * 2f + convexity[Mathf.Min(lCount - 1, k + 1)]) * 0.25f;
            convexity = lSmooth;
        }

        // ---------------------------------------------------------------- queries

        private void Locate(float pS, out int pK, out float pT)
        {
            float lF = (pS - _spine.SOf(i0)) / Spine.STEP;
            pK = Mathf.Clamp(Mathf.FloorToInt(lF), 0, Count - 1);
            pT = Mathf.Clamp01(lF - pK);
            if (pK >= Count - 1) { pK = Count - 1; pT = 0f; }
        }

        public void Frame(float pS, out Vector3 pCenter, out Vector3 pForward, out Vector3 pRight, out Vector3 pUp, out float pWidth)
        {
            int lK;
            float lT;
            Locate(pS, out lK, out lT);
            int lN = Mathf.Min(lK + 1, Count - 1);
            pCenter = Vector3.Lerp(center[lK], center[lN], lT);
            pForward = Vector3.Slerp(fwd[lK], fwd[lN], lT).normalized;
            pRight = Vector3.Slerp(right[lK], right[lN], lT).normalized;
            pUp = Vector3.Slerp(up[lK], up[lN], lT).normalized;
            pWidth = Mathf.Lerp(w[lK], w[lN], lT);
        }

        public float Width(float pS)
        {
            int lK;
            float lT;
            Locate(pS, out lK, out lT);
            return Mathf.Lerp(w[lK], w[Mathf.Min(lK + 1, Count - 1)], lT);
        }

        // Lateral offset of the deck center from the spine.
        public float LateralOffset(float pS)
        {
            int lK;
            float lT;
            Locate(pS, out lK, out lT);
            return Mathf.Lerp(x[lK], x[Mathf.Min(lK + 1, Count - 1)], lT);
        }

        public EdgeKind Edge(float pS, int pSide)
        {
            int lK;
            float lT;
            Locate(pS, out lK, out lT);
            if (lT > 0.5f) lK = Mathf.Min(lK + 1, Count - 1);
            return pSide < 0 ? edgeL[lK] : edgeR[lK];
        }

        public float Convexity(float pS)
        {
            int lK;
            float lT;
            Locate(pS, out lK, out lT);
            return Mathf.Lerp(convexity[lK], convexity[Mathf.Min(lK + 1, Count - 1)], lT);
        }

        public DeckProfile Profile(float pS, out float pTroughT)
        {
            int lK;
            float lT;
            Locate(pS, out lK, out lT);
            if (lT > 0.5f) lK = Mathf.Min(lK + 1, Count - 1);
            pTroughT = troughT[lK];
            return profile[lK];
        }

        // Height offset of the cross-section at lateral t (troughs and gutters dip toward the rail line).
        public float ProfileHeight(float pS, float pLateral)
        {
            float lTrough;
            DeckProfile lProfile = Profile(pS, out lTrough);
            float lHalf = Width(pS) * 0.5f;
            return ProfileAt(lProfile, lTrough, lHalf, pLateral);
        }

        public static float ProfileAt(DeckProfile pProfile, float pTroughT, float pHalf, float pLateral)
        {
            switch (pProfile)
            {
                case DeckProfile.Trough:
                    return -TROUGH_DEPTH * Mathf.Clamp01(1f - Mathf.Abs(pLateral - pTroughT) / (TROUGH_WIDTH * 0.5f));
                case DeckProfile.GutterL:
                    return -TROUGH_DEPTH * Mathf.Clamp01(1f - (pLateral + pHalf) / TROUGH_WIDTH);
                case DeckProfile.GutterR:
                    return -TROUGH_DEPTH * Mathf.Clamp01(1f - (pHalf - pLateral) / TROUGH_WIDTH);
            }
            return 0f;
        }

        public float Lateral(Vector3 pPosition, float pS)
        {
            Vector3 lCenter, lForward, lRight, lUp;
            float lWidth;
            Frame(pS, out lCenter, out lForward, out lRight, out lUp, out lWidth);
            return Vector3.Dot(pPosition - lCenter, lRight);
        }

        public Vector3 SurfacePoint(float pS, float pLateral)
        {
            Vector3 lCenter, lForward, lRight, lUp;
            float lWidth;
            Frame(pS, out lCenter, out lForward, out lRight, out lUp, out lWidth);
            return lCenter + lRight * pLateral + lUp * ProfileHeight(pS, pLateral);
        }

        public Vector3 SurfaceNormal(float pS, float pLateral)
        {
            Vector3 lCenter, lForward, lRight, lUp;
            float lWidth;
            Frame(pS, out lCenter, out lForward, out lRight, out lUp, out lWidth);
            float lSlope = (ProfileHeight(pS, pLateral + 0.25f) - ProfileHeight(pS, pLateral - 0.25f)) / 0.5f;
            return (lUp - lRight * lSlope).normalized;
        }

        // Signed height of a point above the surface (along the deck up) and its lateral coordinate.
        public float HeightAbove(Vector3 pPosition, float pS, out float pLateral)
        {
            Vector3 lCenter, lForward, lRight, lUp;
            float lWidth;
            Frame(pS, out lCenter, out lForward, out lRight, out lUp, out lWidth);
            Vector3 lDelta = pPosition - lCenter;
            pLateral = Vector3.Dot(lDelta, lRight);
            return Vector3.Dot(lDelta, lUp) - ProfileHeight(pS, pLateral);
        }

        public bool IsLipAhead(float pS, float pDistance)
        {
            return pS + pDistance > EndS && endCap;
        }
    }
}
