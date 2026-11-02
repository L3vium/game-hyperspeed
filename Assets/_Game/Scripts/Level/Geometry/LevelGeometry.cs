using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Builds every mesh of a layout : decks (chunked, crumble tiles), cliffs along canyon / wall-run edges.
    public static class LevelGeometry
    {
        public static void Build(LevelLayout pLayout, Transform pRoot, Theme pTheme, BreakDirector pDirector)
        {
            Transform lDecks = new GameObject("Decks").transform;
            lDecks.SetParent(pRoot, false);

            DeckMesher.Materials lMaterials = new DeckMesher.Materials();
            lMaterials.deck = new Material(Mats.Toon(Color.white, pTheme.shade, 0f, 0.15f, true));
            lMaterials.deck.SetFloat("_RoadStreaks", 1f);
            lMaterials.deck.SetFloat("_HalftoneAmount", 0.35f);
            Color lMembrane = pTheme.accent * 1.4f;
            lMembrane.a = 0.55f;
            lMaterials.membrane = new Material(Mats.Glow(lMembrane, false, 0f, 1.2f, false));
            lMaterials.membrane.SetFloat("_StripeFreq", 0.25f);
            lMaterials.membrane.SetFloat("_Scroll", 1.5f);
            lMaterials.membrane.SetFloat("_Cull", 0f);

            IList<Vector2> lCrumble = pDirector != null ? pDirector.CrumbleRanges : null;
            foreach (Deck lDeck in pLayout.decks) DeckMesher.Build(lDeck, pLayout, pTheme, lDecks, lMaterials, lCrumble, pDirector);

            foreach (CliffSpan lCliff in pLayout.cliffs) BuildCliff(pLayout, lCliff, pTheme, lDecks, lMaterials.deck);
        }

        // Rock wall along a deck edge : faceted outside, flat inner face when solid (wall runs need clean normals).
        private static void BuildCliff(LevelLayout pLayout, CliffSpan pCliff, Theme pTheme, Transform pParent, Material pMaterial)
        {
            MeshData lVisual = new MeshData();
            MeshData lCollision = new MeshData();
            Deck lDeck = pCliff.deck;
            float lS0 = Mathf.Max(pCliff.s0, lDeck.StartS);
            float lS1 = Mathf.Min(pCliff.s1, lDeck.EndS);
            int lSteps = Mathf.Max(1, Mathf.RoundToInt((lS1 - lS0) / Spine.STEP));
            float lSeed = lDeck.id * 3.3f + pCliff.side * 11f;
            Vector3 lPrevInLow = Vector3.zero, lPrevInHigh = Vector3.zero, lPrevOutHigh = Vector3.zero, lPrevOutLow = Vector3.zero;
            for (int i = 0; i <= lSteps; i++)
            {
                float lS = Mathf.Lerp(lS0, lS1, i / (float)lSteps);
                Vector3 lC, lF, lR, lU;
                float lW;
                lDeck.Frame(lS, out lC, out lF, out lR, out lU, out lW);
                float lLateral = pCliff.side * (pCliff.inset > 0f ? lW * 0.5f - pCliff.inset : lW * 0.5f + 0.9f);
                float lNoise = Mathf.PerlinNoise(lS * 0.04f + lSeed, 0.5f);
                float lHigh = pCliff.high * (pCliff.solid ? 1f : 0.75f + lNoise * 0.5f);
                Vector3 lInLow = lC + lR * lLateral + lU * pCliff.low;
                Vector3 lInHigh = lC + lR * lLateral + lU * lHigh;
                float lThick = 3f + lNoise * 3f;
                Vector3 lOutHigh = lInHigh + lR * pCliff.side * lThick - lU * lNoise * 2f;
                Vector3 lOutLow = lInLow + lR * pCliff.side * (lThick + 2f) - lU * 4f;
                if (!pCliff.solid)
                {
                    float lJag = (Mathf.PerlinNoise(lS * 0.12f + lSeed, 2f) - 0.5f) * 1.6f;
                    lInHigh += lR * pCliff.side * lJag;
                }
                if (i > 0)
                {
                    Color lFace = Color.Lerp(pTheme.roadSide, pTheme.decorC, 0.3f + 0.4f * lNoise);
                    Color lTop = Color.Lerp(pTheme.roadA, pTheme.roadSide, 0.5f);
                    lVisual.FacetQuad(lPrevInLow, lInLow, lInHigh, lPrevInHigh, -lR * pCliff.side, lFace);
                    lVisual.FacetQuad(lPrevInHigh, lInHigh, lOutHigh, lPrevOutHigh, lU, lTop);
                    lVisual.FacetQuad(lPrevOutHigh, lOutHigh, lOutLow, lPrevOutLow, lR * pCliff.side, lFace * 0.8f);
                    if (pCliff.solid)
                        lCollision.Quad(lPrevInLow, lInLow, lInHigh, lPrevInHigh, -lR * pCliff.side, Color.white);
                }
                lPrevInLow = lInLow;
                lPrevInHigh = lInHigh;
                lPrevOutHigh = lOutHigh;
                lPrevOutLow = lOutLow;
            }
            GameObject lObject = new GameObject("Cliff" + lDeck.id + (pCliff.side < 0 ? "L" : "R"));
            lObject.transform.SetParent(pParent, false);
            lObject.layer = Layers.PROP;
            lObject.AddComponent<MeshFilter>().sharedMesh = lVisual.ToMesh(lObject.name);
            lObject.AddComponent<MeshRenderer>().sharedMaterial = pMaterial;
            if (pCliff.solid) lObject.AddComponent<MeshCollider>().sharedMesh = lCollision.ToMesh(lObject.name + "_col");
        }
    }
}
