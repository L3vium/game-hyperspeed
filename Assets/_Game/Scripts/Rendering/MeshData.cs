using System.Collections.Generic;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Growable mesh buffer for procedural geometry (decks, rocks, cliffs). Quads orient themselves from their normals.
    public class MeshData
    {
        public static readonly Vector2 NO_UV = new Vector2(-1f, 0f);

        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Color> colors = new List<Color>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> triangles = new List<int>();

        public int Count => vertices.Count;

        public int Vertex(Vector3 pPosition, Vector3 pNormal, Color pColor, Vector2 pUv)
        {
            vertices.Add(pPosition);
            normals.Add(pNormal);
            colors.Add(pColor);
            uvs.Add(pUv);
            return vertices.Count - 1;
        }

        // Triangle facing pFacing (winding is flipped if needed).
        public void Tri(int pA, int pB, int pC, Vector3 pFacing)
        {
            Vector3 lFace = Vector3.Cross(vertices[pB] - vertices[pA], vertices[pC] - vertices[pA]);
            if (Vector3.Dot(lFace, pFacing) >= 0f) { triangles.Add(pA); triangles.Add(pB); triangles.Add(pC); }
            else { triangles.Add(pA); triangles.Add(pC); triangles.Add(pB); }
        }

        public void Quad(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pD, Vector3 pNA, Vector3 pNB, Vector3 pNC, Vector3 pND, Color pCA, Color pCB, Color pCC, Color pCD, Vector2 pUA, Vector2 pUB, Vector2 pUC, Vector2 pUD)
        {
            int lA = Vertex(pA, pNA, pCA, pUA);
            int lB = Vertex(pB, pNB, pCB, pUB);
            int lC = Vertex(pC, pNC, pCC, pUC);
            int lD = Vertex(pD, pND, pCD, pUD);
            Vector3 lFacing = pNA + pNB + pNC + pND;
            Tri(lA, lB, lC, lFacing);
            Tri(lA, lC, lD, lFacing);
        }

        public void Quad(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pD, Vector3 pNormal, Color pColor)
        {
            Quad(pA, pB, pC, pD, pNormal, pNormal, pNormal, pNormal, pColor, pColor, pColor, pColor, NO_UV, NO_UV, NO_UV, NO_UV);
        }

        // Flat-shaded quad (faceted rock look) : the normal comes from the face itself, pOutward picks the side.
        public void FacetQuad(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pD, Vector3 pOutward, Color pColor)
        {
            Vector3 lNormal = Vector3.Cross(pB - pA, pD - pA);
            if (lNormal.sqrMagnitude < 1e-8f) lNormal = Vector3.Cross(pC - pB, pD - pB);
            lNormal = lNormal.normalized;
            if (Vector3.Dot(lNormal, pOutward) < 0f) lNormal = -lNormal;
            Quad(pA, pB, pC, pD, lNormal, pColor);
        }

        public void FacetTri(Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pOutward, Color pColor)
        {
            Vector3 lNormal = Vector3.Cross(pB - pA, pC - pA).normalized;
            if (Vector3.Dot(lNormal, pOutward) < 0f) lNormal = -lNormal;
            int lA = Vertex(pA, lNormal, pColor, NO_UV);
            int lB = Vertex(pB, lNormal, pColor, NO_UV);
            int lC = Vertex(pC, lNormal, pColor, NO_UV);
            Tri(lA, lB, lC, lNormal);
        }

        // Appends another mesh transformed (merging rocks into a deck chunk).
        public void Append(Mesh pMesh, Matrix4x4 pMatrix, Color pColor)
        {
            Vector3[] lVertices = pMesh.vertices;
            Vector3[] lNormals = pMesh.normals;
            int[] lTriangles = pMesh.triangles;
            int lOffset = vertices.Count;
            for (int i = 0; i < lVertices.Length; i++)
            {
                vertices.Add(pMatrix.MultiplyPoint3x4(lVertices[i]));
                normals.Add(pMatrix.MultiplyVector(lNormals[i]).normalized);
                colors.Add(pColor);
                uvs.Add(NO_UV);
            }
            for (int i = 0; i < lTriangles.Length; i++) triangles.Add(lTriangles[i] + lOffset);
        }

        public Mesh ToMesh(string pName)
        {
            Mesh lMesh = new Mesh { name = pName };
            lMesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            lMesh.SetVertices(vertices);
            lMesh.SetNormals(normals);
            lMesh.SetColors(colors);
            lMesh.SetUVs(0, uvs);
            lMesh.SetTriangles(triangles, 0);
            lMesh.RecalculateBounds();
            return lMesh;
        }

        public void Clear()
        {
            vertices.Clear();
            normals.Clear();
            colors.Clear();
            uvs.Clear();
            triangles.Clear();
        }
    }
}
