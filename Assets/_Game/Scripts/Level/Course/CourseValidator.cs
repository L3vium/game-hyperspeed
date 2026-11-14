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
            TrackField lField = pLayout.field;
            const int STRIDE = 20;
            for (int i = 0; i < lSpine.Count; i += STRIDE)
            {
                Vector3 lPoint = lSpine.samples[i].position;
                long lKey = Key(Mathf.FloorToInt(lPoint.x / CELL), Mathf.FloorToInt(lPoint.z / CELL));
                List<int> lList;
                if (!_grid.TryGetValue(lKey, out lList)) _grid[lKey] = lList = new List<int>();
                lList.Add(i);
            }

            // Two pieces of track through the same space, in 3D : a body-sized margin around each.
            int lOverlaps = 0;
            float lFirst = float.NaN;
            string lDetail = "";
            for (int i = 0; i < lSpine.Count; i += STRIDE)
            {
                // Light highways are checked by construction (wide, gentle, alternating) : their width would make this
                // search thousands of times slower.
                if (pLayout.InHighway(lSpine.samples[i].s)) continue;
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
                            if (lOther.s - lSpine.samples[i].s < Mathf.PI * lReach * 2.5f) continue;
                            float lMin = lField.TotalHalfWidth(lSpine.samples[i].s) + lField.TotalHalfWidth(lOther.s);
                            Vector3 lDelta = lOther.position - lPoint;
                            if (lDelta.sqrMagnitude < lMin * lMin)
                            {
                                lOverlaps++;
                                if (float.IsNaN(lFirst))
                                {
                                    lFirst = lSpine.samples[i].s;
                                    lDetail = " with s=" + lOther.s.ToString("F0") + " dist=" + lDelta.magnitude.ToString("F0") + " min=" + lMin.ToString("F0");
                                }
                            }
                        }
                    }
                }
            }
            if (lOverlaps > 0) Debug.LogWarning("[HS-GEN] route overlaps itself " + lOverlaps + "x, first at s=" + lFirst.ToString("F0") + lDetail);

            // Pacing : never a long stretch of nothing.
            float lLongest = 0f;
            float lLast = pLayout.startLineS;
            foreach (RouteAct lAct in pLayout.acts)
            {
                lLongest = Mathf.Max(lLongest, lAct.s - lLast);
                lLast = lAct.s;
            }
            Debug.Log("[HS-GEN] longest act-free stretch " + (lLongest / pLayout.profile.cruise).ToString("F1") + "s, dives=" + pDives);
        }

        // Scenery must stay clear of every leg of the route.
        public static bool IsClear(Vector3 pPosition, float pRadius)
        {
            if (_layout == null) return true;
            float lReach = pRadius + 200f;
            int lCells = Mathf.CeilToInt(lReach / CELL);
            int lX = Mathf.FloorToInt(pPosition.x / CELL);
            int lZ = Mathf.FloorToInt(pPosition.z / CELL);
            Spine lSpine = _layout.spine;
            TrackField lField = _layout.field;
            for (int lDx = -lCells; lDx <= lCells; lDx++)
            {
                for (int lDz = -lCells; lDz <= lCells; lDz++)
                {
                    List<int> lList;
                    if (!_grid.TryGetValue(Key(lX + lDx, lZ + lDz), out lList)) continue;
                    foreach (int i in lList)
                    {
                        SpineSample lSample = lSpine.samples[i];
                        float lMin = pRadius + lField.TotalHalfWidth(lSample.s) + 40f;
                        if ((lSample.position - pPosition).sqrMagnitude < lMin * lMin) return false;
                    }
                }
            }
            return true;
        }
    }
}
