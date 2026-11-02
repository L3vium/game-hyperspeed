using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Turns a deck into meshes : banded top with readable edges, rounded lip, strata flanks and a faceted
    // rocky underside (floating island look), boulders along hard edges. Collision uses a separate simple slab.
    public static class DeckMesher
    {
        private const int CHUNK = 24;
        private const int TILE = 5;
        private const int TOP = 16;
        private const int UNDER = 8;
        private const float BAND = 8f;
        private const float EDGE_BAND = 1.1f;
        private const float BEVEL_OUT = 0.55f;
        private const float BEVEL_DOWN = 0.45f;
        private const float COLLIDER_DEPTH = 2.4f;

        private struct Section
        {
            public Vector3 c;
            public Vector3 f;
            public Vector3 r;
            public Vector3 u;
            public float half;
            public float s;
            public DeckProfile profile;
            public float trough;
        }

        public class Materials
        {
            public Material deck;
            public Material membrane;
        }

        private static readonly Vector3[] _top = new Vector3[TOP];
        private static readonly Vector3[] _topNext = new Vector3[TOP];

        public static void Build(Deck pDeck, LevelLayout pLayout, Theme pTheme, Transform pRoot, Materials pMaterials, IList<Vector2> pCrumbleRanges, BreakDirector pDirector)
        {
            Section[] lSections = new Section[pDeck.Count];
            for (int k = 0; k < pDeck.Count; k++) lSections[k] = SectionAt(pDeck, pLayout, k);

            System.Random lRandom = new System.Random(pDeck.id * 7919 + 17);
            int lK = 0;
            int lIndex = 0;
            while (lK < pDeck.Count - 1)
            {
                bool lCrumble = pDeck.surface == DeckSurface.Crumble || InRanges(lSections[lK].s, pCrumbleRanges);
                int lSize = lCrumble ? TILE : CHUNK;
                int lEnd = Mathf.Min(pDeck.Count - 1, lK + lSize);
                // Do not let a chunk straddle into a crumble range : cut at its start.
                if (!lCrumble)
                    for (int k = lK + 1; k < lEnd; k++)
                        if (InRanges(lSections[k].s, pCrumbleRanges)) { lEnd = k; break; }
                BuildChunk(pDeck, pTheme, pRoot, pMaterials, lSections, lK, lEnd, lIndex++, lCrumble, lRandom, pDirector);
                lK = lEnd;
            }
        }

        private static bool InRanges(float pS, IList<Vector2> pRanges)
        {
            if (pRanges == null) return false;
            for (int i = 0; i < pRanges.Count; i++)
                if (pS >= pRanges[i].x && pS <= pRanges[i].y) return true;
            return false;
        }

        private static Section SectionAt(Deck pDeck, LevelLayout pLayout, int pK)
        {
            return new Section
            {
                c = pDeck.center[pK],
                f = pDeck.fwd[pK],
                r = pDeck.right[pK],
                u = pDeck.up[pK],
                half = pDeck.w[pK] * 0.5f,
                s = pLayout.spine.SOf(pDeck.i0 + pK),
                profile = pDeck.profile[pK],
                trough = pDeck.troughT[pK]
            };
        }

        private static float Fraction(int pJ, float pHalf)
        {
            if (pJ == 0) return -1f;
            if (pJ == TOP - 1) return 1f;
            float lBand = Mathf.Min(0.2f, EDGE_BAND / Mathf.Max(1f, pHalf));
            if (pJ == 1) return -1f + lBand;
            if (pJ == TOP - 2) return 1f - lBand;
            return -1f + lBand + (2f - 2f * lBand) * (pJ - 1) / (TOP - 2f);
        }

        private static void TopRow(Section pS, Vector3[] pOut)
        {
            for (int j = 0; j < TOP; j++)
            {
                float lT = Fraction(j, pS.half) * pS.half;
                pOut[j] = pS.c + pS.r * lT + pS.u * Deck.ProfileAt(pS.profile, pS.trough, pS.half, lT);
            }
        }

        private static Vector3 TopNormal(Section pS, float pT)
        {
            float lSlope = (Deck.ProfileAt(pS.profile, pS.trough, pS.half, pT + 0.25f) - Deck.ProfileAt(pS.profile, pS.trough, pS.half, pT - 0.25f)) / 0.5f;
            return (pS.u - pS.r * lSlope).normalized;
        }

        private static Color EdgeColor(EdgeKind pKind, Theme pTheme, Color pCenter)
        {
            switch (pKind)
            {
                case EdgeKind.Lip: return pTheme.roadEdge;
                case EdgeKind.Wall: return Color.Lerp(pTheme.roadSide, pTheme.decorC, 0.4f) * 0.85f;
                case EdgeKind.Open: return pTheme.accent * 1.3f;
            }
            return pCenter;
        }

        private static Color TopColor(Deck pDeck, Theme pTheme, Section pS, int pJ, int pK)
        {
            bool lBand = Mathf.FloorToInt(pS.s / BAND) % 2 == 0;
            Color lCenter;
            switch (pDeck.style)
            {
                case DeckStyle.Road: lCenter = new Color(0.2f, 0.21f, 0.26f); break;
                case DeckStyle.Track: lCenter = new Color(0.32f, 0.28f, 0.27f); break;
                case DeckStyle.Slab: lCenter = lBand ? Color.Lerp(pTheme.roadA, Color.white, 0.25f) : Color.Lerp(pTheme.roadB, Color.white, 0.2f); break;
                case DeckStyle.Membrane: lCenter = Color.white; break;
                default: lCenter = lBand ? pTheme.roadA : pTheme.roadB; break;
            }
            if (pDeck.style == DeckStyle.Rock || pDeck.style == DeckStyle.Slab)
            {
                float lNoise = Mathf.PerlinNoise(pS.s * 0.045f + pDeck.id * 3.1f, pJ * 0.33f);
                lCenter *= 0.93f + lNoise * 0.12f;
            }
            float lT = Fraction(pJ, pS.half) * pS.half;
            if (pS.profile != DeckProfile.Flat && Deck.ProfileAt(pS.profile, pS.trough, pS.half, lT) < -0.25f)
                lCenter = Color.Lerp(lCenter, pTheme.accent, 0.45f);
            if (pJ == 0) return EdgeColor(pDeck.edgeL[pK], pTheme, lCenter);
            if (pJ == TOP - 1) return EdgeColor(pDeck.edgeR[pK], pTheme, lCenter);
            lCenter.a = 1f;
            return lCenter;
        }

        private static Color StrataColor(Theme pTheme, float pDepth, float pNoise)
        {
            int lBand = Mathf.FloorToInt((pDepth + pNoise * 2.2f) / 2.6f);
            Color lColor;
            switch (((lBand % 3) + 3) % 3)
            {
                case 0: lColor = pTheme.roadSide; break;
                case 1: lColor = Color.Lerp(pTheme.roadSide, pTheme.decorC, 0.6f); break;
                default: lColor = Color.Lerp(pTheme.decorB, pTheme.roadSide, 0.5f); break;
            }
            return lColor * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(pDepth / 30f));
        }

        // Underside depth profile across the deck (a = 0 left lip .. 1 right lip) : rounded keel with noise.
        private static Vector3 UnderPoint(Deck pDeck, Section pS, float pA, float pSeed)
        {
            float lSide = pS.half + BEVEL_OUT;
            float lBell = 1f - (2f * pA - 1f) * (2f * pA - 1f);
            float lSideDepth = BEVEL_DOWN + 2.2f;
            float lNoiseA = Mathf.PerlinNoise(pS.s * 0.035f + pSeed, pA * 2.3f);
            float lNoiseB = Mathf.PerlinNoise(pS.s * 0.09f + pSeed * 2f, pA * 5.1f + 3f);
            float lDepth;
            float lLateral = Mathf.Lerp(-lSide, lSide, pA);
            switch (pDeck.style)
            {
                case DeckStyle.Slab:
                case DeckStyle.Road:
                case DeckStyle.Track:
                case DeckStyle.Membrane:
                    lDepth = Mathf.Lerp(lSideDepth, Mathf.Max(lSideDepth, pDeck.underDepth), lBell > 0.05f ? 1f : 0f);
                    break;
                default:
                    float lKeel = pDeck.underDepth * (0.7f + lNoiseA * 0.6f);
                    lDepth = lSideDepth + (lKeel - lSideDepth) * Mathf.Pow(Mathf.Max(0f, lBell), 0.65f);
                    lDepth += (lNoiseB - 0.5f) * 1.6f * lBell;
                    lLateral *= 1f - 0.18f * lBell;
                    lLateral += (lNoiseB - 0.5f) * 1.4f * lBell;
                    break;
            }
            return pS.c + pS.r * lLateral - pS.u * lDepth;
        }

        private static void BuildChunk(Deck pDeck, Theme pTheme, Transform pRoot, Materials pMaterials, Section[] pSections, int pK0, int pK1, int pIndex, bool pCrumble, System.Random pRandom, BreakDirector pDirector)
        {
            MeshData lVisual = new MeshData();
            MeshData lCollision = new MeshData();
            bool lMembrane = pDeck.surface == DeckSurface.Membrane;
            float lSeed = pDeck.id * 5.7f;

            for (int k = pK0; k < pK1; k++)
            {
                Section lA = pSections[k];
                Section lB = pSections[k + 1];
                TopRow(lA, _top);
                TopRow(lB, _topNext);

                // Top surface (smooth normals, banded colors, streak UVs : u lateral 0..1, v = s).
                for (int j = 0; j < TOP - 1; j++)
                {
                    float lFa = Fraction(j, lA.half), lFb = Fraction(j + 1, lA.half);
                    float lGa = Fraction(j, lB.half), lGb = Fraction(j + 1, lB.half);
                    lVisual.Quad(_top[j], _topNext[j], _topNext[j + 1], _top[j + 1],
                        TopNormal(lA, lFa * lA.half), TopNormal(lB, lGa * lB.half), TopNormal(lB, lGb * lB.half), TopNormal(lA, lFb * lA.half),
                        TopColor(pDeck, pTheme, lA, j, k), TopColor(pDeck, pTheme, lB, j, k + 1), TopColor(pDeck, pTheme, lB, j + 1, k + 1), TopColor(pDeck, pTheme, lA, j + 1, k),
                        new Vector2((lFa + 1f) * 0.5f, lA.s), new Vector2((lGa + 1f) * 0.5f, lB.s), new Vector2((lGb + 1f) * 0.5f, lB.s), new Vector2((lFb + 1f) * 0.5f, lA.s));
                    lCollision.Quad(_top[j], _topNext[j], _topNext[j + 1], _top[j + 1], lA.u, Color.white);
                }

                // Collision flanks and bottom : simple slab.
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    int lJ = lSide < 0 ? 0 : TOP - 1;
                    lCollision.Quad(_top[lJ], _topNext[lJ], _topNext[lJ] - lB.u * COLLIDER_DEPTH, _top[lJ] - lA.u * COLLIDER_DEPTH, lA.r * lSide, Color.white);
                }
                lCollision.Quad(_top[0] - lA.u * COLLIDER_DEPTH, _topNext[0] - lB.u * COLLIDER_DEPTH, _topNext[TOP - 1] - lB.u * COLLIDER_DEPTH, _top[TOP - 1] - lA.u * COLLIDER_DEPTH, -lA.u, Color.white);

                if (lMembrane)
                {
                    for (int lSide = -1; lSide <= 1; lSide += 2)
                    {
                        int lJ = lSide < 0 ? 0 : TOP - 1;
                        lVisual.Quad(_top[lJ], _topNext[lJ], _topNext[lJ] - lB.u * 0.5f, _top[lJ] - lA.u * 0.5f, lA.r * lSide, pTheme.accent);
                    }
                    continue;
                }

                // Rounded lip + flanks.
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    int lJ = lSide < 0 ? 0 : TOP - 1;
                    EdgeKind lKind = lSide < 0 ? pDeck.edgeL[k] : pDeck.edgeR[k];
                    Color lEdge = EdgeColor(lKind, pTheme, pTheme.roadA);
                    Vector3 lBevelA = _top[lJ] + lA.r * lSide * BEVEL_OUT - lA.u * BEVEL_DOWN;
                    Vector3 lBevelB = _topNext[lJ] + lB.r * lSide * BEVEL_OUT - lB.u * BEVEL_DOWN;
                    lVisual.Quad(_top[lJ], _topNext[lJ], lBevelB, lBevelA, (lA.u + lA.r * lSide).normalized, (lB.u + lB.r * lSide).normalized, (lB.u * 0.3f + lB.r * lSide).normalized, (lA.u * 0.3f + lA.r * lSide).normalized,
                        lEdge, lEdge, lEdge * 0.8f, lEdge * 0.8f, MeshData.NO_UV, MeshData.NO_UV, MeshData.NO_UV, MeshData.NO_UV);
                    Vector3 lLowA = UnderPoint(pDeck, lA, lSide < 0 ? 0f : 1f, lSeed);
                    Vector3 lLowB = UnderPoint(pDeck, lB, lSide < 0 ? 0f : 1f, lSeed);
                    Color lFlank = StrataColor(pTheme, 1.5f, Mathf.PerlinNoise(lA.s * 0.05f, lSeed));
                    lVisual.FacetQuad(lBevelA, lBevelB, lLowB, lLowA, lA.r * lSide, lFlank);
                }

                // Faceted underside.
                Vector3 lMidA = lA.c - lA.u * pDeck.underDepth * 0.4f;
                for (int m = 0; m < UNDER; m++)
                {
                    float lA0 = m / (float)UNDER, lA1 = (m + 1) / (float)UNDER;
                    Vector3 lP00 = UnderPoint(pDeck, lA, lA0, lSeed);
                    Vector3 lP01 = UnderPoint(pDeck, lA, lA1, lSeed);
                    Vector3 lP10 = UnderPoint(pDeck, lB, lA0, lSeed);
                    Vector3 lP11 = UnderPoint(pDeck, lB, lA1, lSeed);
                    float lDepth = Vector3.Dot(lA.c - (lP00 + lP01) * 0.5f, lA.u);
                    Color lColor = StrataColor(pTheme, lDepth, Mathf.PerlinNoise(lA.s * 0.05f + m, lSeed));
                    lVisual.FacetQuad(lP00, lP10, lP11, lP01, (lP00 + lP01) * 0.5f - lMidA, lColor);
                }

                if (!lMembrane) EdgeRocks(lVisual, pDeck, pTheme, lA, lB, k, pRandom);
            }

            if (pK0 == 0 && pDeck.startCap) Cap(lVisual, lCollision, pDeck, pTheme, pSections[0], -1f, lSeed, lMembrane);
            if (pK1 == pDeck.Count - 1 && pDeck.endCap) Cap(lVisual, lCollision, pDeck, pTheme, pSections[pK1], 1f, lSeed, lMembrane);

            GameObject lChunk = new GameObject("Deck" + pDeck.id + "_" + pIndex);
            lChunk.transform.SetParent(pRoot, false);
            lChunk.layer = lMembrane ? Layers.MEMBRANE : Layers.DECK;
            lChunk.AddComponent<MeshFilter>().sharedMesh = lVisual.ToMesh(lChunk.name);
            lChunk.AddComponent<MeshRenderer>().sharedMaterial = lMembrane ? pMaterials.membrane : pMaterials.deck;
            MeshCollider lCollider = lChunk.AddComponent<MeshCollider>();
            lCollider.sharedMesh = lCollision.ToMesh(lChunk.name + "_col");
            DeckChunk lDeckChunk = DeckChunk.Register(lChunk, pDeck, pSections[pK0].s, pSections[pK1].s, lCollider);
            if (pCrumble && pDirector != null) pDirector.AddTile(lDeckChunk);
        }

        // Front / back face : a jagged broken rock face (fan around a point pushed out along the deck).
        private static void Cap(MeshData pVisual, MeshData pCollision, Deck pDeck, Theme pTheme, Section pS, float pSign, float pSeed, bool pMembrane)
        {
            TopRow(pS, _top);
            Vector3 lOut = pS.f * pSign;
            for (int j = 0; j < TOP - 1; j++)
                pCollision.Quad(_top[j], _top[j + 1], _top[j + 1] - pS.u * COLLIDER_DEPTH, _top[j] - pS.u * COLLIDER_DEPTH, lOut, Color.white);
            if (pMembrane)
            {
                pVisual.Quad(_top[0], _top[TOP - 1], _top[TOP - 1] - pS.u * 0.5f, _top[0] - pS.u * 0.5f, lOut, pTheme.accent);
                return;
            }

            List<Vector3> lRing = new List<Vector3>();
            for (int j = 0; j < TOP; j++) lRing.Add(_top[j]);
            lRing.Add(_top[TOP - 1] + pS.r * BEVEL_OUT - pS.u * BEVEL_DOWN);
            for (int m = UNDER; m >= 0; m--) lRing.Add(UnderPoint(pDeck, pS, m / (float)UNDER, pSeed));
            lRing.Add(_top[0] - pS.r * BEVEL_OUT - pS.u * BEVEL_DOWN);
            Vector3 lCenter = pS.c - pS.u * Mathf.Max(2f, pDeck.underDepth * 0.35f) + lOut * (1.5f + Mathf.PerlinNoise(pS.s, pSeed) * 2.5f);
            Color lRock = Color.Lerp(pTheme.decorC, pTheme.roadSide, 0.4f) * 0.8f;
            for (int i = 0; i < lRing.Count; i++)
            {
                Vector3 lA = lRing[i];
                Vector3 lB = lRing[(i + 1) % lRing.Count];
                // Small jag on each rim vertex so broken faces never look sliced.
                Vector3 lJag = lOut * (Mathf.PerlinNoise(i * 0.7f, pSeed + pS.s) * 0.8f);
                pVisual.FacetTri(lA, lB + lJag, lCenter, lOut, i < TOP - 1 ? Color.Lerp(lRock, pTheme.roadSide, 0.5f) : lRock);
            }
        }

        // Level art follows the edge kind : boulders on hard edges, small tufts on lips.
        private static void EdgeRocks(MeshData pVisual, Deck pDeck, Theme pTheme, Section pA, Section pB, int pK, System.Random pRandom)
        {
            if (pDeck.style != DeckStyle.Rock && pDeck.style != DeckStyle.Slab) return;
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                EdgeKind lKind = lSide < 0 ? pDeck.edgeL[pK] : pDeck.edgeR[pK];
                double lChance = lKind == EdgeKind.Wall ? 0.6 : lKind == EdgeKind.Lip ? 0.22 : 0.0;
                if (pRandom.NextDouble() > lChance) continue;
                float lT = (float)pRandom.NextDouble();
                Vector3 lCenter = Vector3.Lerp(pA.c, pB.c, lT);
                Vector3 lRight = Vector3.Slerp(pA.r, pB.r, lT);
                Vector3 lUp = Vector3.Slerp(pA.u, pB.u, lT);
                float lHalf = Mathf.Lerp(pA.half, pB.half, lT);
                Quaternion lRotation = Quaternion.LookRotation(Vector3.Slerp(pA.f, pB.f, lT), lUp) * Quaternion.Euler((float)pRandom.NextDouble() * 20f - 10f, (float)pRandom.NextDouble() * 360f, (float)pRandom.NextDouble() * 20f - 10f);
                int lVariant = pRandom.Next(0, 6);
                if (lKind == EdgeKind.Wall)
                {
                    float lSize = 1.1f + (float)pRandom.NextDouble() * 1.7f;
                    Vector3 lBase = lCenter + lRight * lSide * (lHalf + 0.35f + lSize * 0.25f) - lUp * 0.6f;
                    Color lColor = Color.Lerp(pTheme.decorC, pTheme.roadSide, (float)pRandom.NextDouble()) * 0.9f;
                    RockSculptor.Place(pVisual, RockSculptor.Boulder(lVariant), lBase, lRotation, new Vector3(lSize * 1.3f, lSize, lSize * 1.2f), lColor);
                }
                else
                {
                    float lSize = 0.35f + (float)pRandom.NextDouble() * 0.35f;
                    Vector3 lBase = lCenter + lRight * lSide * (lHalf + 0.2f) - lUp * 0.25f;
                    RockSculptor.Place(pVisual, RockSculptor.Shard(lVariant), lBase, lRotation, new Vector3(lSize, lSize * 1.4f, lSize), Color.Lerp(pTheme.decorA, pTheme.roadEdge, 0.3f));
                }
            }
        }
    }
}
