using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Faceted procedural rocks (noise-displaced icospheres) shared by deck edges, cliffs, debris and scenery.
    public static class RockSculptor
    {
        private const int VARIANTS = 6;

        private static Mesh[] _boulders;
        private static Mesh[] _shards;

        public static Mesh Boulder(int pIndex)
        {
            if (_boulders == null) _boulders = Build(0.32f, 1.0f, 101);
            return _boulders[Mathf.Abs(pIndex) % VARIANTS];
        }

        // Tall, thin variants (crystals, spires, broken pillars).
        public static Mesh Shard(int pIndex)
        {
            if (_shards == null) _shards = Build(0.22f, 2.6f, 307);
            return _shards[Mathf.Abs(pIndex) % VARIANTS];
        }

        private static Mesh[] Build(float pNoise, float pStretch, int pSeed)
        {
            Mesh[] lMeshes = new Mesh[VARIANTS];
            for (int i = 0; i < VARIANTS; i++) lMeshes[i] = Sculpt(pNoise, pStretch, pSeed + i * 13);
            return lMeshes;
        }

        // Unit-sized rock, origin at its base center.
        private static Mesh Sculpt(float pNoise, float pStretch, int pSeed)
        {
            List<Vector3> lPoints;
            List<int> lFaces;
            Icosphere(out lPoints, out lFaces);
            float lOffset = pSeed * 1.37f;
            for (int i = 0; i < lPoints.Count; i++)
            {
                Vector3 lP = lPoints[i];
                float lN = Mathf.PerlinNoise(lP.x * 1.6f + lOffset, lP.z * 1.6f + lP.y * 0.7f) * 2f - 1f;
                float lN2 = Mathf.PerlinNoise(lP.y * 2.7f + lOffset * 2f, lP.x * 2.3f - lP.z) * 2f - 1f;
                lP *= 1f + lN * pNoise + lN2 * pNoise * 0.5f;
                lP.y *= pStretch * (lP.y < 0f ? 0.45f : 1f);
                lPoints[i] = lP;
            }
            float lMinY = float.MaxValue;
            foreach (Vector3 lP in lPoints) lMinY = Mathf.Min(lMinY, lP.y);

            MeshData lData = new MeshData();
            for (int f = 0; f < lFaces.Count; f += 3)
            {
                Vector3 lA = lPoints[lFaces[f]] - Vector3.up * lMinY;
                Vector3 lB = lPoints[lFaces[f + 1]] - Vector3.up * lMinY;
                Vector3 lC = lPoints[lFaces[f + 2]] - Vector3.up * lMinY;
                Vector3 lCenter = (lA + lB + lC) / 3f;
                lData.FacetTri(lA * 0.5f, lB * 0.5f, lC * 0.5f, lCenter - Vector3.up * (-lMinY), Color.white);
            }
            return lData.ToMesh("Rock");
        }

        private static void Icosphere(out List<Vector3> pPoints, out List<int> pFaces)
        {
            float lT = (1f + Mathf.Sqrt(5f)) * 0.5f;
            pPoints = new List<Vector3>
            {
                new Vector3(-1, lT, 0), new Vector3(1, lT, 0), new Vector3(-1, -lT, 0), new Vector3(1, -lT, 0),
                new Vector3(0, -1, lT), new Vector3(0, 1, lT), new Vector3(0, -1, -lT), new Vector3(0, 1, -lT),
                new Vector3(lT, 0, -1), new Vector3(lT, 0, 1), new Vector3(-lT, 0, -1), new Vector3(-lT, 0, 1)
            };
            for (int i = 0; i < pPoints.Count; i++) pPoints[i] = pPoints[i].normalized;
            pFaces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            // One subdivision : 80 faces, enough for chunky stylized rocks.
            Dictionary<long, int> lMid = new Dictionary<long, int>();
            List<int> lNew = new List<int>();
            for (int f = 0; f < pFaces.Count; f += 3)
            {
                int lA = pFaces[f], lB = pFaces[f + 1], lC = pFaces[f + 2];
                int lAb = Mid(pPoints, lMid, lA, lB);
                int lBc = Mid(pPoints, lMid, lB, lC);
                int lCa = Mid(pPoints, lMid, lC, lA);
                lNew.AddRange(new[] { lA, lAb, lCa, lB, lBc, lAb, lC, lCa, lBc, lAb, lBc, lCa });
            }
            pFaces = lNew;
        }

        private static int Mid(List<Vector3> pPoints, Dictionary<long, int> pCache, int pA, int pB)
        {
            long lKey = pA < pB ? ((long)pA << 32) + pB : ((long)pB << 32) + pA;
            int lIndex;
            if (pCache.TryGetValue(lKey, out lIndex)) return lIndex;
            pPoints.Add(((pPoints[pA] + pPoints[pB]) * 0.5f).normalized);
            lIndex = pPoints.Count - 1;
            pCache[lKey] = lIndex;
            return lIndex;
        }

        // Merges one rock into a buffer : base at pPosition, oriented by pRotation, scaled by pSize.
        public static void Place(MeshData pData, Mesh pRock, Vector3 pPosition, Quaternion pRotation, Vector3 pSize, Color pColor)
        {
            pData.Append(pRock, Matrix4x4.TRS(pPosition, pRotation, pSize), pColor);
        }
    }
}
