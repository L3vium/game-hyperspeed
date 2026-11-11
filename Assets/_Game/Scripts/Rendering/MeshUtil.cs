using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Small procedural meshes.
    public static class MeshUtil
    {
        // Flat V along -Z for unit length : apex at the origin, two arms sweeping back at +/- pAngle degrees, curving a
        // little outward. Each arm is a ribbon from pWidth0 at the apex to pWidth1 at its end. UV.x runs along the arm,
        // vertex alpha fades out toward the ends. Double sided, in the XZ plane.
        public static Mesh Wake(float pAngle, float pLength, float pWidth0, float pWidth1, int pSegments)
        {
            System.Collections.Generic.List<Vector3> lVertices = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.List<Color> lColors = new System.Collections.Generic.List<Color>();
            System.Collections.Generic.List<Vector2> lUvs = new System.Collections.Generic.List<Vector2>();
            System.Collections.Generic.List<int> lTriangles = new System.Collections.Generic.List<int>();
            float lSin = Mathf.Sin(pAngle * Mathf.Deg2Rad);
            float lCos = Mathf.Cos(pAngle * Mathf.Deg2Rad);
            for (int lSide = -1; lSide <= 1; lSide += 2)
            {
                int lBase = lVertices.Count;
                for (int i = 0; i <= pSegments; i++)
                {
                    float lT = i / (float)pSegments;
                    Vector3 lCenter = new Vector3(lSide * (lSin * lT + 0.12f * lT * lT), 0f, -lCos * lT) * pLength;
                    Vector3 lTangent = new Vector3(lSide * (lSin + 0.24f * lT), 0f, -lCos).normalized;
                    Vector3 lAcross = new Vector3(-lTangent.z, 0f, lTangent.x) * lSide;
                    float lWidth = Mathf.Lerp(pWidth0, pWidth1, lT) * pLength * 0.5f;
                    float lAlpha = Mathf.Pow(1f - lT, 1.4f) * Mathf.Clamp01(lT * 12f + 0.3f);
                    lVertices.Add(lCenter - lAcross * lWidth);
                    lVertices.Add(lCenter + lAcross * lWidth);
                    lColors.Add(new Color(1f, 1f, 1f, lAlpha));
                    lColors.Add(new Color(1f, 1f, 1f, lAlpha * 0.35f));
                    lUvs.Add(new Vector2(lT, 0f));
                    lUvs.Add(new Vector2(lT, 1f));
                }
                for (int i = 0; i < pSegments; i++)
                {
                    int lA = lBase + i * 2, lB = lA + 1, lC = lA + 2, lD = lA + 3;
                    lTriangles.AddRange(new[] { lA, lC, lB, lB, lC, lD, lA, lB, lC, lB, lD, lC });
                }
            }
            Mesh lMesh = new Mesh { name = "Wake" };
            lMesh.SetVertices(lVertices);
            lMesh.SetColors(lColors);
            lMesh.SetUVs(0, lUvs);
            lMesh.SetTriangles(lTriangles, 0);
            lMesh.RecalculateNormals();
            lMesh.RecalculateBounds();
            return lMesh;
        }

        // Open cone along -Z : wide ring at the origin, narrow ring at -pLength. UV.y = 0 front, 1 back.
        public static Mesh Cone(float pFrontRadius, float pBackRadius, float pLength, int pSides)
        {
            Vector3[] lVertices = new Vector3[(pSides + 1) * 2];
            Vector3[] lNormals = new Vector3[lVertices.Length];
            Vector2[] lUvs = new Vector2[lVertices.Length];
            int[] lTriangles = new int[pSides * 12];
            for (int i = 0; i <= pSides; i++)
            {
                float lAngle = i / (float)pSides * Mathf.PI * 2f;
                Vector3 lDir = new Vector3(Mathf.Cos(lAngle), Mathf.Sin(lAngle), 0f);
                lVertices[i * 2] = lDir * pFrontRadius;
                lVertices[i * 2 + 1] = lDir * pBackRadius - Vector3.forward * pLength;
                lNormals[i * 2] = lDir;
                lNormals[i * 2 + 1] = lDir;
                lUvs[i * 2] = new Vector2(i / (float)pSides, 0f);
                lUvs[i * 2 + 1] = new Vector2(i / (float)pSides, 1f);
            }
            int lIndex = 0;
            for (int i = 0; i < pSides; i++)
            {
                int lA = i * 2, lB = i * 2 + 1, lC = i * 2 + 2, lD = i * 2 + 3;
                // Double sided.
                lTriangles[lIndex++] = lA; lTriangles[lIndex++] = lB; lTriangles[lIndex++] = lC;
                lTriangles[lIndex++] = lC; lTriangles[lIndex++] = lB; lTriangles[lIndex++] = lD;
                lTriangles[lIndex++] = lA; lTriangles[lIndex++] = lC; lTriangles[lIndex++] = lB;
                lTriangles[lIndex++] = lC; lTriangles[lIndex++] = lD; lTriangles[lIndex++] = lB;
            }
            Mesh lMesh = new Mesh { name = "Cone" };
            lMesh.vertices = lVertices;
            lMesh.normals = lNormals;
            lMesh.uv = lUvs;
            lMesh.triangles = lTriangles;
            lMesh.RecalculateBounds();
            return lMesh;
        }

        // Flat ring in the XY plane (expanding shockwaves, ring flashes).
        public static Mesh Ring(float pInner, float pOuter, int pSides)
        {
            Vector3[] lVertices = new Vector3[(pSides + 1) * 2];
            Vector2[] lUvs = new Vector2[lVertices.Length];
            int[] lTriangles = new int[pSides * 12];
            for (int i = 0; i <= pSides; i++)
            {
                float lAngle = i / (float)pSides * Mathf.PI * 2f;
                Vector3 lDir = new Vector3(Mathf.Cos(lAngle), Mathf.Sin(lAngle), 0f);
                lVertices[i * 2] = lDir * pInner;
                lVertices[i * 2 + 1] = lDir * pOuter;
                lUvs[i * 2] = new Vector2(i / (float)pSides, 0f);
                lUvs[i * 2 + 1] = new Vector2(i / (float)pSides, 1f);
            }
            int lIndex = 0;
            for (int i = 0; i < pSides; i++)
            {
                int lA = i * 2, lB = i * 2 + 1, lC = i * 2 + 2, lD = i * 2 + 3;
                lTriangles[lIndex++] = lA; lTriangles[lIndex++] = lB; lTriangles[lIndex++] = lC;
                lTriangles[lIndex++] = lC; lTriangles[lIndex++] = lB; lTriangles[lIndex++] = lD;
                lTriangles[lIndex++] = lA; lTriangles[lIndex++] = lC; lTriangles[lIndex++] = lB;
                lTriangles[lIndex++] = lC; lTriangles[lIndex++] = lD; lTriangles[lIndex++] = lB;
            }
            Mesh lMesh = new Mesh { name = "Ring" };
            lMesh.vertices = lVertices;
            lMesh.uv = lUvs;
            lMesh.triangles = lTriangles;
            lMesh.RecalculateNormals();
            lMesh.RecalculateBounds();
            return lMesh;
        }
    }
}
