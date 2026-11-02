using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Com.IsartDigital.Hyperspeed
{
    // Scanned rocks (Poly Haven, CC0, decimated in Blender : Art/Environment/Rocks) drawn cel-shaded with their own
    // albedo and tinted per placement to the theme. Placements are merged per chunk : one mesh, one submesh per variant.
    // Without the assets every caller falls back to RockSculptor.
    public static class RockKit
    {
        public const int BOULDERS = 6;
        public const int CLIFFS = 4;
        private const int PARTS = BOULDERS * 2 + CLIFFS;

        private class Variant
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector2[] uvs;
            public int[] triangles;
            public Vector3 size;
            public Texture texture;
        }

        private static Variant[] _variants;
        private static readonly Dictionary<long, Material> _materials = new Dictionary<long, Material>();

        public static bool Available
        {
            get
            {
                Load();
                return _variants != null;
            }
        }

        private static void Load()
        {
            if (_variants != null) return;
            GameAssets lAssets = GameAssets.Instance;
            if (lAssets.rockMeshes == null || lAssets.rockMeshes.Length < BOULDERS + CLIFFS) return;
            if (lAssets.rockMeshesLow == null || lAssets.rockMeshesLow.Length < BOULDERS) return;
            Variant[] lVariants = new Variant[PARTS];
            for (int i = 0; i < PARTS; i++)
            {
                // Parts : high boulders, cliffs, then low boulders (same textures as their high versions).
                int lTexture = i < BOULDERS + CLIFFS ? i : i - BOULDERS - CLIFFS;
                Mesh lMesh = i < BOULDERS + CLIFFS ? lAssets.rockMeshes[i] : lAssets.rockMeshesLow[i - BOULDERS - CLIFFS];
                if (lMesh == null || !lMesh.isReadable) return;
                lVariants[i] = new Variant
                {
                    vertices = lMesh.vertices,
                    normals = lMesh.normals,
                    uvs = lMesh.uv,
                    triangles = lMesh.triangles,
                    size = lMesh.bounds.size,
                    texture = lAssets.rockTextures != null && lAssets.rockTextures.Length > lTexture ? lAssets.rockTextures[lTexture] : null
                };
            }
            _variants = lVariants;
        }

        private static Material MaterialFor(int pPart, Color pShade)
        {
            long lKey = pPart * 1000003L + pShade.GetHashCode();
            Material lMaterial;
            if (_materials.TryGetValue(lKey, out lMaterial) && lMaterial != null) return lMaterial;
            lMaterial = new Material(Mats.Toon(Color.white, pShade, 0f, 0.25f, true));
            if (_variants[pPart].texture != null) lMaterial.SetTexture("_BaseMap", _variants[pPart].texture);
            lMaterial.name = "Rock" + pPart;
            _materials[lKey] = lMaterial;
            return lMaterial;
        }

        // Theme color -> vertex tint that keeps the scanned detail readable under the cel shading.
        public static Color Tint(Color pTheme, float pAmount = 0.6f) => Color.Lerp(Color.white, pTheme, pAmount) * 1.15f;

        public class Batch
        {
            private readonly MeshData[] _parts = new MeshData[PARTS];

            public bool Empty
            {
                get
                {
                    foreach (MeshData lPart in _parts) if (lPart != null && lPart.Count > 0) return false;
                    return true;
                }
            }

            // Boulder whose largest horizontal extent is pSize meters (natural proportions, height scaled by pHeight).
            public void Boulder(int pVariant, bool pLow, Vector3 pBase, Quaternion pRotation, float pSize, float pHeight, Color pTint)
            {
                int lVariant = Mathf.Abs(pVariant) % BOULDERS;
                Place(pLow ? BOULDERS + CLIFFS + lVariant : lVariant, pBase, pRotation, pSize, pHeight, pTint);
            }

            // Cliff / rock face (large scenery pieces).
            public void Cliff(int pVariant, Vector3 pBase, Quaternion pRotation, float pSize, float pHeight, Color pTint)
            {
                Place(BOULDERS + Mathf.Abs(pVariant) % CLIFFS, pBase, pRotation, pSize, pHeight, pTint);
            }

            private void Place(int pPart, Vector3 pBase, Quaternion pRotation, float pSize, float pHeight, Color pTint)
            {
                Variant lVariant = _variants[pPart];
                float lScale = pSize / Mathf.Max(0.01f, Mathf.Max(lVariant.size.x, lVariant.size.z));
                if (_parts[pPart] == null) _parts[pPart] = new MeshData();
                _parts[pPart].Append(lVariant.vertices, lVariant.normals, lVariant.uvs, lVariant.triangles,
                    Matrix4x4.TRS(pBase, pRotation, new Vector3(lScale, lScale * pHeight, lScale)), pTint);
            }

            public GameObject Build(string pName, Transform pParent, int pLayer, Color pShade, bool pShadows)
            {
                List<Vector3> lVertices = new List<Vector3>();
                List<Vector3> lNormals = new List<Vector3>();
                List<Color> lColors = new List<Color>();
                List<Vector2> lUvs = new List<Vector2>();
                List<int[]> lSubmeshes = new List<int[]>();
                List<Material> lMaterials = new List<Material>();
                for (int i = 0; i < PARTS; i++)
                {
                    MeshData lPart = _parts[i];
                    if (lPart == null || lPart.Count == 0) continue;
                    int lOffset = lVertices.Count;
                    lVertices.AddRange(lPart.vertices);
                    lNormals.AddRange(lPart.normals);
                    lColors.AddRange(lPart.colors);
                    lUvs.AddRange(lPart.uvs);
                    int[] lTriangles = lPart.triangles.ToArray();
                    for (int t = 0; t < lTriangles.Length; t++) lTriangles[t] += lOffset;
                    lSubmeshes.Add(lTriangles);
                    lMaterials.Add(MaterialFor(i, pShade));
                }
                if (lSubmeshes.Count == 0) return null;

                Mesh lMesh = new Mesh { name = pName };
                lMesh.indexFormat = lVertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                lMesh.SetVertices(lVertices);
                lMesh.SetNormals(lNormals);
                lMesh.SetColors(lColors);
                lMesh.SetUVs(0, lUvs);
                lMesh.subMeshCount = lSubmeshes.Count;
                for (int i = 0; i < lSubmeshes.Count; i++) lMesh.SetTriangles(lSubmeshes[i], i);
                lMesh.RecalculateBounds();

                GameObject lObject = new GameObject(pName);
                lObject.transform.SetParent(pParent, true);
                lObject.layer = pLayer;
                lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
                MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
                lRenderer.sharedMaterials = lMaterials.ToArray();
                lRenderer.shadowCastingMode = pShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                return lObject;
            }
        }
    }
}
