using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Ground chunk, kept so the late stages can make the world fall away behind the runner.
    public class TerrainChunk
    {
        public float s0;
        public float s1;
        public GameObject gameObject;
        public MeshCollider collider;
    }

    // Faceted flat-colour mesh of the terrain field : one colour per quad, hard patches, cliffs a darker colour.
    public static class TerrainBaker
    {
        private const int INTERIOR = 14;
        private const int CHUNK_ROWS = 40;

        public static readonly List<TerrainChunk> Chunks = new List<TerrainChunk>();

        public static void Bake(LevelLayout pLayout, Theme pTheme, Transform pRoot)
        {
            Chunks.Clear();
            TerrainField lField = pLayout.field;
            Spine lSpine = lField.spine;
            float lRowStep = Mathf.Max(12f, 15f * pLayout.profile.k);

            List<float> lRows = new List<float>();
            for (float lS = lSpine.StartS; lS < lSpine.EndS; lS += lRowStep) lRows.Add(lS);
            lRows.Add(lSpine.EndS);
            foreach (float lCrease in lField.creases)
            {
                lRows.Add(lCrease);
                lRows.Add(lCrease + Spine.STEP);
            }
            lRows.Sort();
            for (int i = lRows.Count - 1; i > 0; i--)
                if (lRows[i] - lRows[i - 1] < 0.5f) lRows.RemoveAt(i);

            Transform lParent = new GameObject("Terrain").transform;
            lParent.SetParent(pRoot, false);
            Material lMaterial = Mats.Flat(pTheme.shade, true);

            int lColumns = INTERIOR + 1 + 6;
            for (int lFirst = 0; lFirst < lRows.Count - 1; lFirst += CHUNK_ROWS)
            {
                int lLast = Mathf.Min(lRows.Count - 1, lFirst + CHUNK_ROWS);
                BuildChunk(lField, pTheme, lRows, lFirst, lLast, lColumns, lParent, lMaterial, pLayout.profile.k);
            }
            BuildCap(lField, pTheme, lRows[0], lParent, lMaterial, -1f);
            BuildCap(lField, pTheme, lRows[lRows.Count - 1], lParent, lMaterial, 1f);
        }

        // Bare end of the ribbon : a wall down into the fog, seen from nowhere in play but from the replay camera.
        private static void BuildCap(TerrainField pField, Theme pTheme, float pS, Transform pParent, Material pMaterial, float pDirection)
        {
            MeshData lData = new MeshData();
            SpineSample lSample = pField.spine.Evaluate(pS);
            Vector3 lForward = lSample.Forward * pDirection;
            float lHalf = pField.TotalHalfWidth(pS);
            float lTop = lSample.position.y + pField.bermHeight;
            float lBottom = lSample.position.y - pField.skirtDrop;
            Vector3 lA = pField.spine.Point(pS, -lHalf, lTop);
            Vector3 lB = pField.spine.Point(pS, lHalf, lTop);
            Vector3 lC = pField.spine.Point(pS, lHalf, lBottom);
            Vector3 lD = pField.spine.Point(pS, -lHalf, lBottom);
            lData.Quad(lA, lB, lC, lD, lForward, pTheme.roadSide);
            GameObject lObject = new GameObject("Cap");
            lObject.transform.SetParent(pParent, false);
            lObject.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("TerrainCap");
            lObject.AddComponent<MeshRenderer>().sharedMaterial = pMaterial;
        }

        private static float ColumnLateral(TerrainField pField, int pIndex, int pColumns, float pHalf, float pOffset)
        {
            float lBerm = pField.bermWidth;
            float lSkirt = pField.skirtWidth;
            int lLast = pColumns - 1;
            if (pIndex == 0) return pOffset - pHalf - lBerm - lSkirt;
            if (pIndex == 1) return pOffset - pHalf - lBerm;
            if (pIndex == 2) return pOffset - pHalf - lBerm * 0.5f;
            if (pIndex == lLast) return pOffset + pHalf + lBerm + lSkirt;
            if (pIndex == lLast - 1) return pOffset + pHalf + lBerm;
            if (pIndex == lLast - 2) return pOffset + pHalf + lBerm * 0.5f;
            float lT = (pIndex - 3) / (float)INTERIOR;
            return pOffset + Mathf.Lerp(-pHalf, pHalf, lT);
        }

        private static void BuildChunk(TerrainField pField, Theme pTheme, List<float> pRows, int pFirst, int pLast, int pColumns, Transform pParent, Material pMaterial, float pK)
        {
            Spine lSpine = pField.spine;
            int lRowCount = pLast - pFirst + 1;
            Vector3[] lGrid = new Vector3[lRowCount * pColumns];
            Vector2[] lRouteUv = new Vector2[lRowCount * pColumns];
            Vector3 lOrigin = lSpine.Point(pRows[pFirst], 0f, lSpine.Height(pRows[pFirst]));
            for (int lRow = 0; lRow < lRowCount; lRow++)
            {
                float lS = pRows[pFirst + lRow];
                float lHalf = pField.HalfWidth(lS);
                float lOffset = pField.Offset(lS);
                SpineSample lSample = lSpine.Evaluate(lS);
                for (int lColumn = 0; lColumn < pColumns; lColumn++)
                {
                    float lLateral = ColumnLateral(pField, lColumn, pColumns, lHalf, lOffset);
                    Vector3 lPoint = lSample.position + lSample.Right * lLateral;
                    lPoint.y = pField.Height(lS, lLateral);
                    lGrid[lRow * pColumns + lColumn] = lPoint - lOrigin;
                    lRouteUv[lRow * pColumns + lColumn] = new Vector2(lLateral, lS);
                }
            }

            int lQuads = (lRowCount - 1) * (pColumns - 1);
            Vector3[] lVertices = new Vector3[lQuads * 6];
            Vector3[] lNormals = new Vector3[lQuads * 6];
            Color[] lColors = new Color[lQuads * 6];
            Vector2[] lUvs = new Vector2[lQuads * 6];
            int[] lTriangles = new int[lQuads * 6];
            int lWrite = 0;
            int lCenter = pColumns / 2;
            float lPatch = 240f * pK;
            for (int lRow = 0; lRow < lRowCount - 1; lRow++)
            {
                float lS0 = pRows[pFirst + lRow];
                float lS1 = pRows[pFirst + lRow + 1];
                float lMidS = (lS0 + lS1) * 0.5f;
                bool lIsRoad = pField.road[lSpine.IndexOf(lMidS)] > 0.5f;
                for (int lColumn = 0; lColumn < pColumns - 1; lColumn++)
                {
                    int lIa = lRow * pColumns + lColumn;
                    int lIb = lIa + 1;
                    int lId = (lRow + 1) * pColumns + lColumn;
                    int lIc = lId + 1;
                    Vector3 lA = lGrid[lIa];
                    Vector3 lB = lGrid[lIb];
                    Vector3 lC = lGrid[lIc];
                    Vector3 lD = lGrid[lId];
                    Vector2 lUa = lRouteUv[lIa];
                    Vector2 lUb = lRouteUv[lIb];
                    Vector2 lUc = lRouteUv[lIc];
                    Vector2 lUd = lRouteUv[lId];
                    bool lFlip = (lRow + lColumn) % 2 == 0;
                    Color lBase = QuadColor(pTheme, lMidS, lColumn, lCenter, lPatch, lIsRoad, pColumns);
                    if (lFlip)
                    {
                        Tri(lVertices, lNormals, lColors, lUvs, lTriangles, ref lWrite, lA, lD, lB, lUa, lUd, lUb, lBase, pTheme);
                        Tri(lVertices, lNormals, lColors, lUvs, lTriangles, ref lWrite, lB, lD, lC, lUb, lUd, lUc, lBase, pTheme);
                    }
                    else
                    {
                        Tri(lVertices, lNormals, lColors, lUvs, lTriangles, ref lWrite, lA, lD, lC, lUa, lUd, lUc, lBase, pTheme);
                        Tri(lVertices, lNormals, lColors, lUvs, lTriangles, ref lWrite, lA, lC, lB, lUa, lUc, lUb, lBase, pTheme);
                    }
                }
            }

            GameObject lObject = new GameObject("Ground" + pFirst);
            lObject.transform.SetParent(pParent, false);
            lObject.transform.position = lOrigin;
            lObject.layer = Layers.DECK;
            Mesh lMesh = new Mesh { name = "TerrainVisual" };
            lMesh.vertices = lVertices;
            lMesh.normals = lNormals;
            lMesh.colors = lColors;
            lMesh.uv = lUvs;
            lMesh.triangles = lTriangles;
            lMesh.RecalculateBounds();
            lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
            MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = pMaterial;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            int[] lIndices = new int[lQuads * 6];
            int lIndex = 0;
            for (int lRow = 0; lRow < lRowCount - 1; lRow++)
            {
                for (int lColumn = 0; lColumn < pColumns - 1; lColumn++)
                {
                    int lA = lRow * pColumns + lColumn;
                    int lB = lA + 1;
                    int lD = (lRow + 1) * pColumns + lColumn;
                    int lC = lD + 1;
                    bool lFlip = (lRow + lColumn) % 2 == 0;
                    if (lFlip)
                    {
                        lIndices[lIndex++] = lA; lIndices[lIndex++] = lD; lIndices[lIndex++] = lB;
                        lIndices[lIndex++] = lB; lIndices[lIndex++] = lD; lIndices[lIndex++] = lC;
                    }
                    else
                    {
                        lIndices[lIndex++] = lA; lIndices[lIndex++] = lD; lIndices[lIndex++] = lC;
                        lIndices[lIndex++] = lA; lIndices[lIndex++] = lC; lIndices[lIndex++] = lB;
                    }
                }
            }
            Mesh lCollision = new Mesh { name = "TerrainCollision" };
            lCollision.vertices = lGrid;
            lCollision.triangles = lIndices;
            MeshCollider lCollider = lObject.AddComponent<MeshCollider>();
            lCollider.sharedMesh = lCollision;

            Chunks.Add(new TerrainChunk { s0 = pRows[pFirst], s1 = pRows[pLast], gameObject = lObject, collider = lCollider });
        }

        private static Color QuadColor(Theme pTheme, float pS, int pColumn, int pCenter, float pPatch, bool pRoad, int pColumns)
        {
            if (pColumn <= 1 || pColumn >= pColumns - 3) return pTheme.roadSide;
            if (pColumn == 2 || pColumn == pColumns - 4) return Color.Lerp(pTheme.roadSide, pTheme.roadA, 0.55f);
            if (pRoad)
            {
                int lFromCenter = Mathf.Abs(pColumn - pCenter + (pColumn < pCenter ? 0 : 0));
                if (pColumn == pCenter - 1 || pColumn == pCenter) return pTheme.road;
                if (lFromCenter == 2) return pTheme.roadEdge;
            }
            float lNoise = Mathf.PerlinNoise(pS / pPatch + 11.3f, pColumn * 0.31f + 3.7f);
            Color lColor = lNoise < 0.4f ? pTheme.roadB : lNoise > 0.6f ? pTheme.groundC : pTheme.roadA;
            // Checker of huge squares : motion reads on a ground with no texture.
            int lBand = Mathf.FloorToInt(pS / (pPatch * 0.45f));
            if (((lBand + pColumn / 3) & 1) == 0) lColor = Color.Lerp(lColor, Color.white, 0.07f);
            return lColor;
        }

        private static void Tri(Vector3[] pVertices, Vector3[] pNormals, Color[] pColors, Vector2[] pUvs, int[] pTriangles, ref int pWrite, Vector3 pA, Vector3 pB, Vector3 pC, Vector2 pUa, Vector2 pUb, Vector2 pUc, Color pBase, Theme pTheme)
        {
            Vector3 lCross = Vector3.Cross(pB - pA, pC - pA);
            Vector3 lNormal = lCross.sqrMagnitude > 1e-6f ? lCross.normalized : Vector3.up;
            // Winding always faces up : cliffs and skirts stay visible from outside.
            Color lColor = lNormal.y < 0.5f ? pTheme.roadSide : pBase;
            for (int i = 0; i < 3; i++)
            {
                Vector3 lPoint = i == 0 ? pA : i == 1 ? pB : pC;
                pVertices[pWrite] = lPoint;
                pNormals[pWrite] = lNormal;
                pColors[pWrite] = lColor;
                pUvs[pWrite] = i == 0 ? pUa : i == 1 ? pUb : pUc;
                pTriangles[pWrite] = pWrite;
                pWrite++;
            }
        }
    }
}
