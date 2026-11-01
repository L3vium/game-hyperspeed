using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Small procedural meshes.
    public static class MeshUtil
    {
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
