using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Piece of track mesh, kept so the late stages can make the world fall away behind the runner.
    public class TrackChunk
    {
        public float s0;
        public float s1;
        public GameObject gameObject;
        public MeshCollider collider;
        public BlockLook look;
    }

    // Solid extruded blocks from the track field : a painted top, leaning rideable sides, thick layered flanks, a rough
    // underside and end faces. Each block is closed, so gaps and offsets read as separate chunks of land.
    public static class TrackBaker
    {
        private const int FLOOR_COLUMNS = 8;
        private const int WALL_COLUMNS = 4;
        private const int MOUNTAIN_COLUMNS = 9;
        private const int CHUNK_ROWS = 36;
        private const float MAX_ROW = 26f;
        private const float MIN_ROW = 6f;
        private const float ROW_ANGLE = 5f;

        public static readonly List<TrackChunk> Chunks = new List<TrackChunk>();

        public static void Bake(LevelLayout pLayout, Theme pTheme, Transform pRoot)
        {
            Chunks.Clear();
            Transform lParent = new GameObject("Track").transform;
            lParent.SetParent(pRoot, false);
            Material lMaterial = Mats.Flat(pTheme.shade, true);
            TrackField lField = pLayout.field;
            foreach (TrackBlock lBlock in lField.blocks) BakeBlock(lField, lBlock, pTheme, lParent, lMaterial);
            Chunks.Sort((pA, pB) => pA.s1.CompareTo(pB.s1));
        }

        // Rows follow the curvature : tight in a loop, long on a straight.
        private static List<float> Rows(Spine pSpine, TrackBlock pBlock)
        {
            List<float> lRows = new List<float>();
            float lS = pBlock.s0;
            lRows.Add(lS);
            SpineSample lLast = pSpine.Evaluate(lS);
            float lSince = 0f;
            while (lS < pBlock.s1 - 0.01f)
            {
                lS = Mathf.Min(pBlock.s1, lS + Spine.STEP);
                lSince += Spine.STEP;
                SpineSample lNow = pSpine.Evaluate(lS);
                float lAngle = Mathf.Max(Vector3.Angle(lLast.forward, lNow.forward), Vector3.Angle(lLast.up, lNow.up));
                if (lS >= pBlock.s1 - 0.01f || lSince >= MAX_ROW || (lSince >= MIN_ROW && lAngle >= ROW_ANGLE))
                {
                    lRows.Add(lS);
                    lLast = lNow;
                    lSince = 0f;
                }
            }
            return lRows;
        }

        private static void BakeBlock(TrackField pField, TrackBlock pBlock, Theme pTheme, Transform pParent, Material pMaterial)
        {
            List<float> lRows = Rows(pField.spine, pBlock);
            for (int lFirst = 0; lFirst < lRows.Count - 1; lFirst += CHUNK_ROWS)
            {
                int lLast = Mathf.Min(lRows.Count - 1, lFirst + CHUNK_ROWS);
                BuildChunk(pField, pBlock, pTheme, lRows, lFirst, lLast, pParent, pMaterial);
            }
        }

        private static int WallColumns(TrackBlock pBlock) => pBlock.mountain ? MOUNTAIN_COLUMNS : WALL_COLUMNS;

        // Height of a side at t (0 at the floor's edge, 1 at the crest). A mountain is rocky : its slope wanders along the
        // route but always starts flat and steepens, with no step anywhere between it and the floor.
        private static float SideHeight(TrackBlock pBlock, float pHeight, float pT, float pS, int pSide)
        {
            float lBase = pHeight * Mathf.Pow(pT, 1.5f);
            if (!pBlock.mountain) return lBase;
            float lNoise = Mathf.PerlinNoise(pS * 0.0042f + pBlock.seed * 1.3f + pSide * 17.1f, pT * 2.3f + pSide * 5.7f) - 0.5f;
            float lRidge = Mathf.PerlinNoise(pS * 0.011f + pSide * 3.3f, pBlock.seed * 0.7f) - 0.5f;
            return lBase * (1f + lNoise * 0.5f * pT + lRidge * 0.35f * pT * pT);
        }

        // Cross-section, left to right along the top, then back under : (x along the block width, y above its floor).
        private static Vector2[] Section(TrackBlock pBlock, float pS, out int pTopCount)
        {
            List<Vector2> lPoints = new List<Vector2>(48);
            float lHalf = pBlock.halfWidth;
            float lRunL = pBlock.WallRun(pBlock.wallLeft);
            float lRunR = pBlock.WallRun(pBlock.wallRight);
            int lCols = WallColumns(pBlock);
            float lCrestL = pBlock.mountain ? pBlock.wallLeft * 0.3f : 5f;
            float lCrestR = pBlock.mountain ? pBlock.wallRight * 0.3f : 5f;

            if (pBlock.wallLeft > 0.01f)
            {
                lPoints.Add(new Vector2(-lHalf - lRunL - lCrestL, SideHeight(pBlock, pBlock.wallLeft, 1f, pS, -1) * (pBlock.mountain ? 0.92f : 1f)));
                for (int i = lCols; i > 0; i--)
                {
                    float lT = i / (float)lCols;
                    lPoints.Add(new Vector2(-lHalf - lRunL * lT, SideHeight(pBlock, pBlock.wallLeft, lT, pS, -1)));
                }
            }
            else lPoints.Add(new Vector2(-lHalf - 1.6f, -1.4f));
            for (int i = 0; i <= FLOOR_COLUMNS; i++) lPoints.Add(new Vector2(Mathf.Lerp(-lHalf, lHalf, i / (float)FLOOR_COLUMNS), 0f));
            if (pBlock.wallRight > 0.01f)
            {
                for (int i = 1; i <= lCols; i++)
                {
                    float lT = i / (float)lCols;
                    lPoints.Add(new Vector2(lHalf + lRunR * lT, SideHeight(pBlock, pBlock.wallRight, lT, pS, 1)));
                }
                lPoints.Add(new Vector2(lHalf + lRunR + lCrestR, SideHeight(pBlock, pBlock.wallRight, 1f, pS, 1) * (pBlock.mountain ? 0.92f : 1f)));
            }
            else lPoints.Add(new Vector2(lHalf + 1.6f, -1.4f));
            pTopCount = lPoints.Count;

            // Flanks and underside : rough, narrower toward the bottom.
            float lRight = lPoints[lPoints.Count - 1].x;
            float lLeft = lPoints[0].x;
            float lDepth = pBlock.thickness;
            float lN = Mathf.PerlinNoise(pS * 0.013f + pBlock.seed * 0.37f, 3.1f);
            float lM = Mathf.PerlinNoise(pS * 0.021f + pBlock.seed * 0.91f, 7.7f);
            float lOuter = Mathf.Max(lRight, -lLeft);
            lPoints.Add(new Vector2(lOuter * Mathf.Lerp(0.97f, 1.04f, lM), -lDepth * 0.32f));
            lPoints.Add(new Vector2(lOuter * Mathf.Lerp(0.82f, 0.96f, lN), -lDepth * 0.66f));
            lPoints.Add(new Vector2(lOuter * Mathf.Lerp(0.55f, 0.72f, lM), -lDepth * Mathf.Lerp(0.85f, 1.15f, lN)));
            lPoints.Add(new Vector2(-lOuter * Mathf.Lerp(0.55f, 0.72f, lN), -lDepth * Mathf.Lerp(0.85f, 1.15f, lM)));
            lPoints.Add(new Vector2(-lOuter * Mathf.Lerp(0.82f, 0.96f, lM), -lDepth * 0.66f));
            lPoints.Add(new Vector2(-lOuter * Mathf.Lerp(0.97f, 1.04f, lN), -lDepth * 0.32f));
            return lPoints.ToArray();
        }

        private static void BuildChunk(TrackField pField, TrackBlock pBlock, Theme pTheme, List<float> pRows, int pFirst, int pLast, Transform pParent, Material pMaterial)
        {
            Spine lSpine = pField.spine;
            int lRowCount = pLast - pFirst + 1;
            SpineSample lBase = lSpine.Evaluate(pRows[pFirst]);
            Vector3 lOrigin = lBase.position;
            float lCos = pBlock.Cos;
            float lSin = pBlock.Sin;

            int lTop;
            int lColumns = Section(pBlock, pRows[pFirst], out lTop).Length;
            Vector3[] lGrid = new Vector3[lRowCount * lColumns];
            Vector3[] lRowUp = new Vector3[lRowCount];
            Vector3[] lRowForward = new Vector3[lRowCount];
            for (int lRow = 0; lRow < lRowCount; lRow++)
            {
                float lS = pRows[pFirst + lRow];
                SpineSample lSample = lSpine.Evaluate(lS);
                Vector3 lRight = lSample.Right;
                Vector3 lWidth = lRight * lCos - lSample.up * lSin;
                Vector3 lHeight = lSample.up * lCos + lRight * lSin;
                Vector3 lCenter = lSample.position + lRight * pBlock.offset - lSample.up * pBlock.drop - lOrigin;
                Vector2[] lSection = Section(pBlock, lS, out lTop);
                for (int lColumn = 0; lColumn < lColumns; lColumn++)
                    lGrid[lRow * lColumns + lColumn] = lCenter + lWidth * lSection[lColumn].x + lHeight * lSection[lColumn].y;
                lRowUp[lRow] = lHeight;
                lRowForward[lRow] = lSample.forward;
            }

            MeshData lData = new MeshData();
            int lWallColumns = pBlock.wallLeft > 0.01f ? WallColumns(pBlock) + 1 : 1;
            int lFloorFirst = lWallColumns;
            int lFloorLast = lFloorFirst + FLOOR_COLUMNS - 1;
            for (int lRow = 0; lRow < lRowCount - 1; lRow++)
            {
                float lS0 = pRows[pFirst + lRow];
                float lS1 = pRows[pFirst + lRow + 1];
                for (int lColumn = 0; lColumn < lColumns; lColumn++)
                {
                    int lNext = (lColumn + 1) % lColumns;
                    Vector3 lA = lGrid[lRow * lColumns + lColumn];
                    Vector3 lB = lGrid[lRow * lColumns + lNext];
                    Vector3 lC = lGrid[(lRow + 1) * lColumns + lNext];
                    Vector3 lD = lGrid[(lRow + 1) * lColumns + lColumn];
                    Vector3 lFace = Vector3.Cross(lD - lA, lB - lA);
                    bool lFloor = lColumn >= lFloorFirst && lColumn <= lFloorLast;
                    if (lFloor)
                    {
                        // Smooth along the route : a loop stays a clean curve instead of a stack of facets.
                        Vector3 lN0 = lRowUp[lRow];
                        Vector3 lN1 = lRowUp[lRow + 1];
                        float lX0 = Mathf.Lerp(-pBlock.halfWidth, pBlock.halfWidth, (lColumn - lFloorFirst) / (float)FLOOR_COLUMNS) + pBlock.offset;
                        float lX1 = Mathf.Lerp(-pBlock.halfWidth, pBlock.halfWidth, (lColumn + 1 - lFloorFirst) / (float)FLOOR_COLUMNS) + pBlock.offset;
                        Color lColor = FloorColor(pBlock, pTheme, (lS0 + lS1) * 0.5f, lColumn - lFloorFirst);
                        lData.Quad(lA, lB, lC, lD, lN0, lN0, lN1, lN1, lColor, lColor, lColor, lColor, new Vector2(lX0, lS0), new Vector2(lX1, lS0), new Vector2(lX1, lS1), new Vector2(lX0, lS1));
                    }
                    else
                    {
                        Color lColor = lColumn < lTop - 1 ? WallColor(pBlock, pTheme, (lS0 + lS1) * 0.5f, lColumn < lFloorFirst ? lFloorFirst - lColumn : lColumn - lFloorLast) : FlankColor(pTheme, lColumn - (lTop - 1), lColumns - lTop, pBlock.seed);
                        lData.FacetQuad(lA, lB, lC, lD, lFace, lColor);
                    }
                }
            }
            List<Vector3> lCapTriangles = new List<Vector3>();
            if (pFirst == 0) CapTriangles(lCapTriangles, lGrid, 0, lColumns, lTop, lFloorFirst, lRowUp[0], pBlock.thickness * 0.32f, -lRowForward[0]);
            if (pLast == pRows.Count - 1) CapTriangles(lCapTriangles, lGrid, (lRowCount - 1) * lColumns, lColumns, lTop, lFloorFirst, lRowUp[lRowCount - 1], pBlock.thickness * 0.32f, lRowForward[lRowCount - 1]);
            Color lCapColor = Color.Lerp(pTheme.roadSide, pTheme.decorB, 0.25f);
            for (int i = 0; i < lCapTriangles.Count; i += 3)
            {
                pData_FacetTri(lData, lCapTriangles[i], lCapTriangles[i + 1], lCapTriangles[i + 2], lCapColor);
            }

            GameObject lObject = new GameObject("Block");
            lObject.transform.SetParent(pParent, false);
            lObject.transform.position = lOrigin;
            lObject.layer = Layers.DECK;
            lObject.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Track");
            MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = pMaterial;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Collision : the same section as a welded grid, outward facing, with the end faces of the block.
            List<int> lIndices = new List<int>((lRowCount - 1) * lColumns * 6 + 12 * lColumns);
            for (int lRow = 0; lRow < lRowCount - 1; lRow++)
            {
                for (int lColumn = 0; lColumn < lColumns; lColumn++)
                {
                    int lNext = (lColumn + 1) % lColumns;
                    int lA = lRow * lColumns + lColumn;
                    int lB = lRow * lColumns + lNext;
                    int lD = (lRow + 1) * lColumns + lColumn;
                    int lC = (lRow + 1) * lColumns + lNext;
                    lIndices.Add(lA); lIndices.Add(lD); lIndices.Add(lB);
                    lIndices.Add(lB); lIndices.Add(lD); lIndices.Add(lC);
                }
            }
            // The end faces, with the channel between the walls left open.
            List<Vector3> lCollisionVertices = new List<Vector3>(lGrid);
            foreach (Vector3 lPoint in lCapTriangles)
            {
                lIndices.Add(lCollisionVertices.Count);
                lCollisionVertices.Add(lPoint);
            }
            Mesh lCollision = new Mesh { name = "TrackCollision" };
            lCollision.indexFormat = lCollisionVertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            lCollision.SetVertices(lCollisionVertices);
            lCollision.triangles = lIndices.ToArray();
            MeshCollider lCollider = lObject.AddComponent<MeshCollider>();
            lCollider.sharedMesh = lCollision;

            Chunks.Add(new TrackChunk { s0 = pRows[pFirst], s1 = pRows[pLast], gameObject = lObject, collider = lCollider, look = pBlock.look });
        }

        // Triangle already wound the right way (see CapTriangles).
        private static void pData_FacetTri(MeshData pData, Vector3 pA, Vector3 pB, Vector3 pC, Color pColor)
        {
            Vector3 lNormal = Vector3.Cross(pB - pA, pC - pA).normalized;
            pData.FacetTri(pA, pB, pC, lNormal, pColor);
        }

        // End face of a block, wound to face pOutward. A band under the whole top line, straight down from each point so the
        // open channel between the walls stays open, then the part under it as a fan.
        private static void CapTriangles(List<Vector3> pOut, Vector3[] pGrid, int pStart, int pColumns, int pTop, int pFloorColumn, Vector3 pUp, float pDepth, Vector3 pOutward)
        {
            Vector3 lFloor = pGrid[pStart + pFloorColumn];
            Vector3 Down(Vector3 pPoint) => pPoint - pUp * (Vector3.Dot(pPoint - lFloor, pUp) + pDepth);
            for (int i = 0; i < pTop - 1; i++)
            {
                Vector3 lA = pGrid[pStart + i];
                Vector3 lB = pGrid[pStart + i + 1];
                Vector3 lC = Down(lB);
                Vector3 lD = Down(lA);
                AddFacing(pOut, lA, lB, lC, pOutward);
                AddFacing(pOut, lA, lC, lD, pOutward);
            }
            Vector3 lCenter = Vector3.zero;
            int lCount = pColumns - pTop;
            for (int i = pTop; i < pColumns; i++) lCenter += pGrid[pStart + i];
            lCenter /= Mathf.Max(1, lCount);
            Vector3 lPrevious = Down(pGrid[pStart + pTop - 1]);
            for (int i = pTop; i < pColumns; i++)
            {
                Vector3 lNext = pGrid[pStart + i];
                AddFacing(pOut, lCenter, lPrevious, lNext, pOutward);
                lPrevious = lNext;
            }
            Vector3 lLeft = Down(pGrid[pStart]);
            AddFacing(pOut, lCenter, lPrevious, lLeft, pOutward);
            AddFacing(pOut, lCenter, lLeft, Down(pGrid[pStart + pTop - 1]), pOutward);
        }

        private static void AddFacing(List<Vector3> pOut, Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pOutward)
        {
            pOut.Add(pA);
            if (Vector3.Dot(Vector3.Cross(pB - pA, pC - pA), pOutward) >= 0f)
            {
                pOut.Add(pB);
                pOut.Add(pC);
            }
            else
            {
                pOut.Add(pC);
                pOut.Add(pB);
            }
        }

        private static Color FloorColor(TrackBlock pBlock, Theme pTheme, float pS, int pColumn)
        {
            int lBand = Mathf.FloorToInt(pS / 18f);
            bool lCheck = ((lBand + pColumn) & 1) == 0;
            if (pBlock.look == BlockLook.Road)
            {
                if (pColumn == 0 || pColumn == FLOOR_COLUMNS - 1) return (lBand & 1) == 0 ? pTheme.roadEdge : pTheme.roadSide;
                if ((pColumn == FLOOR_COLUMNS / 2 - 1 || pColumn == FLOOR_COLUMNS / 2) && (lBand & 3) == 0) return Color.Lerp(pTheme.road, Color.white, 0.55f);
                return pTheme.road;
            }
            float lNoise = Mathf.PerlinNoise(pS / 140f + 11.3f + pBlock.seed, pColumn * 0.37f + 3.7f);
            Color lColor = lNoise < 0.42f ? pTheme.roadB : lNoise > 0.6f ? pTheme.groundC : pTheme.roadA;
            // No checker : flat colour, the shader adds soft patches and mown chevrons.
            if (pBlock.look == BlockLook.Slab && (pColumn == 0 || pColumn == FLOOR_COLUMNS - 1)) lColor = Color.Lerp(lColor, pTheme.roadEdge, 0.65f);
            return lColor;
        }

        // Rideable sides read as such : a painted kerb, brighter toward the top. A mountain is strata of rock, ground at
        // its foot, a pale crest, bands along the route so the slide reads fast.
        private static Color WallColor(TrackBlock pBlock, Theme pTheme, float pS, int pStep)
        {
            if (pBlock.mountain)
            {
                float lH = pStep / (float)MOUNTAIN_COLUMNS;
                int lStrip = Mathf.FloorToInt(pS / 30f);
                Color lRock = lH < 0.25f ? Color.Lerp(pTheme.roadA, pTheme.wall, lH * 4f)
                    : lH < 0.7f ? Color.Lerp(pTheme.wall, pTheme.decorB, (lH - 0.25f) / 0.45f)
                    : Color.Lerp(pTheme.decorB, Color.Lerp(pTheme.fog, Color.white, 0.5f), (lH - 0.7f) / 0.3f);
                if (((lStrip + pStep) & 1) == 0) lRock = Color.Lerp(lRock, Color.black, 0.08f);
                return lRock;
            }
            int lBand = Mathf.FloorToInt(pS / 16f);
            if (pStep >= WALL_COLUMNS) return (lBand & 1) == 0 ? pTheme.roadEdge : Color.Lerp(pTheme.wall, Color.white, 0.2f);
            float lT = pStep / (float)WALL_COLUMNS;
            Color lColor = Color.Lerp(pTheme.roadA, pTheme.wall, Mathf.Clamp01(lT * 1.6f));
            if ((lBand & 1) == 0) lColor = Color.Lerp(lColor, Color.white, 0.1f);
            return lColor;
        }

        // Layers of rock, each block slightly different.
        private static Color FlankColor(Theme pTheme, int pIndex, int pCount, int pSeed)
        {
            switch ((pIndex + pSeed % 2) % 4)
            {
                case 0: return pTheme.roadSide;
                case 1: return Color.Lerp(pTheme.roadSide, pTheme.decorB, 0.5f);
                case 2: return Color.Lerp(pTheme.roadSide, Color.black, 0.28f);
                default: return Color.Lerp(pTheme.wall, pTheme.roadSide, 0.4f);
            }
        }
    }
}
