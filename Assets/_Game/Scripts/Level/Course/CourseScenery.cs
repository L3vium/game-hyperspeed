using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Enormous flat-colour shapes all around the route : mountains, towers, floating blocks. No collider, closed meshes.
    public static class CourseScenery
    {
        public static void Build(LevelLayout pLayout, Theme pTheme, Transform pRoot, int pSeed)
        {
            Transform lParent = new GameObject("Scenery").transform;
            lParent.SetParent(pRoot, false);
            System.Random lRandom = new System.Random(pSeed);
            Spine lSpine = pLayout.spine;
            float lK = 1f;
            float lChunk = 900f;
            Material lMaterial = Mats.Flat(pTheme.shade, false);
            for (float lS0 = lSpine.StartS; lS0 < lSpine.EndS; lS0 += lChunk)
            {
                MeshData lData = new MeshData();
                Vector3 lOrigin = lSpine.Evaluate(lS0).position;
                int lCount = lRandom.Next(6, 10);
                for (int i = 0; i < lCount; i++)
                {
                    float lS = lS0 + (float)lRandom.NextDouble() * lChunk;
                    int lSide = lRandom.Next(0, 2) * 2 - 1;
                    float lHalf = pLayout.field.TotalHalfWidth(lS);
                    bool lFar = lRandom.NextDouble() < 0.45;
                    float lSize = Rand(lRandom, lFar ? 500f : 160f, lFar ? 1100f : 420f) * lK;
                    float lLateral = lSide * (lHalf + lSize * 0.55f + Rand(lRandom, 40f, lFar ? 1500f : 260f) * lK);
                    Vector3 lBase = lSpine.Evaluate(lS).position + Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(lSpine.Forward(lS), Vector3.up).normalized) * lLateral;
                    if (!CourseValidator.IsClear(lBase, lSize * 0.6f)) continue;
                    Color lColor = i % 3 == 0 ? pTheme.decorA : i % 3 == 1 ? pTheme.decorB : pTheme.decorC;
                    if (lFar) lColor = Color.Lerp(lColor, pTheme.fog, 0.35f);
                    Shape(lData, pTheme, lBase - lOrigin - Vector3.up * lSize * 0.3f, lSize, lColor, lRandom);
                }
                if (lData.Count == 0) continue;
                GameObject lObject = new GameObject("Scenery");
                lObject.transform.SetParent(lParent, false);
                lObject.transform.position = lOrigin;
                lObject.layer = Layers.DEBRIS;
                lObject.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Scenery");
                MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
                lRenderer.sharedMaterial = lMaterial;
                lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static float Rand(System.Random pRandom, float pMin, float pMax) => pMin + (float)pRandom.NextDouble() * (pMax - pMin);

        private static void Shape(MeshData pData, Theme pTheme, Vector3 pPosition, float pSize, Color pColor, System.Random pRandom)
        {
            Quaternion lYaw = Quaternion.Euler(0f, Rand(pRandom, 0f, 360f), 0f);
            switch (pTheme.decor)
            {
                case DecorKind.City:
                case DecorKind.Industrial:
                    Tower(pData, pPosition, pSize, pColor, lYaw, pRandom);
                    break;
                case DecorKind.Ice:
                case DecorKind.Night:
                    pData.Append(RockSculptor.Shard(pRandom.Next(0, 6)), Matrix4x4.TRS(pPosition, lYaw, new Vector3(pSize * 0.5f, pSize * 0.8f, pSize * 0.5f)), pColor);
                    break;
                case DecorKind.Space:
                case DecorKind.Void:
                    Vector3 lFloat = pPosition + Vector3.up * Rand(pRandom, 0.3f, 1.2f) * pSize;
                    pData.Append(RockSculptor.Boulder(pRandom.Next(0, 6)), Matrix4x4.TRS(lFloat, lYaw * Quaternion.Euler(Rand(pRandom, -30f, 30f), 0f, Rand(pRandom, -30f, 30f)), Vector3.one * pSize * 0.6f), pColor);
                    break;
                default:
                    pData.Append(RockSculptor.Boulder(pRandom.Next(0, 6)), Matrix4x4.TRS(pPosition, lYaw, new Vector3(pSize, pSize * Rand(pRandom, 0.7f, 1.3f), pSize)), pColor);
                    break;
            }
        }

        private static void Tower(MeshData pData, Vector3 pPosition, float pSize, Color pColor, Quaternion pYaw, System.Random pRandom)
        {
            float lWidth = pSize * Rand(pRandom, 0.25f, 0.5f);
            float lHeight = pSize * Rand(pRandom, 1f, 2.2f);
            MeshData lPart = new MeshData();
            GateKit.Box(lPart, new Vector3(0f, lHeight * 0.5f, 0f), new Vector3(lWidth, lHeight, lWidth), pColor);
            GateKit.Box(lPart, new Vector3(0f, lHeight * 1.08f, 0f), new Vector3(lWidth * 0.6f, lHeight * 0.16f, lWidth * 0.6f), Color.Lerp(pColor, Color.white, 0.25f));
            Matrix4x4 lMatrix = Matrix4x4.TRS(pPosition, pYaw, Vector3.one);
            int lOffset = pData.Count;
            for (int i = 0; i < lPart.Count; i++)
                pData.Vertex(lMatrix.MultiplyPoint3x4(lPart.vertices[i]), lMatrix.MultiplyVector(lPart.normals[i]), lPart.colors[i], lPart.uvs[i]);
            foreach (int lIndex in lPart.triangles) pData.triangles.Add(lIndex + lOffset);
        }
    }
}
