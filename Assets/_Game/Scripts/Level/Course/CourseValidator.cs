using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Checks a stage against the design rules and answers "is this spot of the map free ?" for the scenery.
    public static class CourseValidator
    {
        private const float CELL = 250f;
        private static readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>();
        private static LevelLayout _layout;

        private static long Key(int pX, int pZ) => ((long)pX << 32) ^ (uint)pZ;

        public static void Check(LevelLayout pLayout, int pDives)
        {
            _layout = pLayout;
            _grid.Clear();
            Spine lSpine = pLayout.spine;
            TerrainField lField = pLayout.field;
            const int STRIDE = 20;
            for (int i = 0; i < lSpine.Count; i += STRIDE)
            {
                Vector3 lPoint = lSpine.samples[i].position;
                long lKey = Key(Mathf.FloorToInt(lPoint.x / CELL), Mathf.FloorToInt(lPoint.z / CELL));
                List<int> lList;
                if (!_grid.TryGetValue(lKey, out lList)) _grid[lKey] = lList = new List<int>();
                lList.Add(i);
            }

            int lOverlaps = 0;
            float lFirst = float.NaN;
            for (int i = 0; i < lSpine.Count; i += STRIDE)
            {
                Vector3 lPoint = lSpine.samples[i].position;
                float lReach = lField.TotalHalfWidth(lSpine.samples[i].s);
                int lCells = Mathf.CeilToInt(lReach * 2f / CELL) + 1;
                int lX = Mathf.FloorToInt(lPoint.x / CELL);
                int lZ = Mathf.FloorToInt(lPoint.z / CELL);
                for (int lDx = -lCells; lDx <= lCells; lDx++)
                {
                    for (int lDz = -lCells; lDz <= lCells; lDz++)
                    {
                        List<int> lList;
                        if (!_grid.TryGetValue(Key(lX + lDx, lZ + lDz), out lList)) continue;
                        foreach (int j in lList)
                        {
                            if (j <= i) continue;
                            SpineSample lOther = lSpine.samples[j];
                            if (lOther.s - lSpine.samples[i].s < Mathf.PI * lReach * 1.1f) continue;
                            float lMin = lField.HalfWidth(lSpine.samples[i].s) + lField.HalfWidth(lOther.s) + lField.bermWidth * 2f;
                            Vector3 lDelta = lOther.position - lPoint;
                            lDelta.y = 0f;
                            if (lDelta.sqrMagnitude < lMin * lMin)
                            {
                                lOverlaps++;
                                if (float.IsNaN(lFirst)) lFirst = lSpine.samples[i].s;
                            }
                        }
                    }
                }
            }
            if (lOverlaps > 0) Debug.LogWarning("[HS-GEN] route overlaps itself " + lOverlaps + "x, first at s=" + lFirst.ToString("F0"));
            if (pDives < 2) Debug.LogWarning("[HS-GEN] only " + pDives + " dive zones");
        }

        // Scenery must stay clear of every leg of the route.
        public static bool IsClear(Vector3 pPosition, float pRadius)
        {
            if (_layout == null) return true;
            float lReach = pRadius + 450f * Mathf.Max(1f, _layout.profile.k);
            int lCells = Mathf.CeilToInt(lReach / CELL);
            int lX = Mathf.FloorToInt(pPosition.x / CELL);
            int lZ = Mathf.FloorToInt(pPosition.z / CELL);
            Spine lSpine = _layout.spine;
            TerrainField lField = _layout.field;
            for (int lDx = -lCells; lDx <= lCells; lDx++)
            {
                for (int lDz = -lCells; lDz <= lCells; lDz++)
                {
                    List<int> lList;
                    if (!_grid.TryGetValue(Key(lX + lDx, lZ + lDz), out lList)) continue;
                    foreach (int i in lList)
                    {
                        SpineSample lSample = lSpine.samples[i];
                        Vector3 lDelta = lSample.position - pPosition;
                        lDelta.y = 0f;
                        float lMin = pRadius + lField.TotalHalfWidth(lSample.s) + 20f;
                        if (lDelta.sqrMagnitude < lMin * lMin) return false;
                    }
                }
            }
            return true;
        }
    }
}
