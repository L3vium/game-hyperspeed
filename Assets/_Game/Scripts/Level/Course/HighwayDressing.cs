using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // What makes 20 km/s readable (2026-11-12) : lit posts along both edges of a light highway every few hundred metres
    // (they stream past like rain), colossal arches across the road every few kilometres, and glowing monoliths
    // kilometres tall far out on the plains for parallax. Merged meshes, no collider : nothing here can be touched.
    public static class HighwayDressing
    {
        private const float POST_EVERY = 320f;
        private const float ARCH_EVERY = 4200f;
        private const float LANDMARK_EVERY = 38000f;
        private const float CHUNK = 12000f;

        public static void Build(LevelLayout pLayout, Theme pTheme, Transform pRoot)
        {
            if (pLayout.highways.Count == 0) return;
            Transform lParent = new GameObject("HighwayDressing").transform;
            lParent.SetParent(pRoot, false);
            Material lPostMaterial = Mats.Glow(Color.Lerp(pTheme.accent, Color.white, 0.45f) * 3.2f, false, 0f, 0f, true);
            Material lArchMaterial = Mats.Glow(Color.Lerp(pTheme.accent, new Color(0.6f, 0.9f, 1f), 0.5f) * 2.6f, false, 0f, 0f, true);
            Material lStone = Mats.Flat(pTheme.shade, false, Color.Lerp(pTheme.decorC, new Color(0.15f, 0.12f, 0.2f), 0.5f));
            System.Random lRandom = new System.Random(pLayout.highways.Count * 7919 + 13);
            foreach (Vector2 lHighway in pLayout.highways)
            {
                for (float lC0 = lHighway.x; lC0 < lHighway.y; lC0 += CHUNK)
                {
                    float lC1 = Mathf.Min(lHighway.y, lC0 + CHUNK);
                    Vector3 lOrigin = pLayout.spine.Evaluate(lC0).position;
                    MeshData lPosts = new MeshData();
                    MeshData lArches = new MeshData();
                    MeshData lStones = new MeshData();
                    for (float lS = lC0; lS < lC1; lS += POST_EVERY)
                    {
                        SpineSample lSample = pLayout.spine.Evaluate(lS);
                        float lHalf = pLayout.field.HalfWidth(lS);
                        for (int lSide = -1; lSide <= 1; lSide += 2)
                        {
                            Vector3 lFoot = pLayout.GroundPoint(lS, pLayout.field.CenterLateral(lS) + lSide * lHalf * 0.96f) - lOrigin;
                            Quaternion lRotation = Quaternion.LookRotation(lSample.forward, lSample.up);
                            lPosts.Append(Box, Matrix4x4.TRS(lFoot + lSample.up * 45f, lRotation, new Vector3(10f, 90f, 24f)), Color.white);
                        }
                        // A colossal arch across the whole road every few kilometres.
                        if (Mathf.Repeat(lS - lHighway.x, ARCH_EVERY) < POST_EVERY)
                        {
                            Vector3 lCenter = pLayout.GroundPoint(lS, pLayout.field.CenterLateral(lS)) - lOrigin;
                            Quaternion lRotation = Quaternion.LookRotation(lSample.forward, lSample.up);
                            float lHeight = 700f;
                            for (int lSide = -1; lSide <= 1; lSide += 2)
                                lArches.Append(Box, Matrix4x4.TRS(lCenter + lSample.Right * lSide * lHalf * 1.05f + lSample.up * lHeight * 0.5f, lRotation, new Vector3(60f, lHeight, 60f)), Color.white);
                            lArches.Append(Box, Matrix4x4.TRS(lCenter + lSample.up * lHeight, lRotation, new Vector3(lHalf * 2.2f, 50f, 60f)), Color.white);
                        }
                        // Monoliths far out on the plains : slow parallax at an absurd speed.
                        if (Mathf.Repeat(lS - lHighway.x, LANDMARK_EVERY) < POST_EVERY)
                        {
                            for (int lSide = -1; lSide <= 1; lSide += 2)
                            {
                                float lOut = lHalf + 6000f + (float)lRandom.NextDouble() * 18000f;
                                Vector3 lBase = pLayout.GroundPoint(lS, pLayout.field.CenterLateral(lS)) + lSample.Right * lSide * lOut - lOrigin - lSample.up * 1500f;
                                float lTall = 3000f + (float)lRandom.NextDouble() * 5000f;
                                lStones.Append(Box, Matrix4x4.TRS(lBase + Vector3.up * lTall * 0.5f, Quaternion.Euler(0f, (float)lRandom.NextDouble() * 90f, (float)lRandom.NextDouble() * 10f - 5f), new Vector3(lTall * 0.18f, lTall, lTall * 0.12f)), Color.white);
                                lPosts.Append(Box, Matrix4x4.TRS(lBase + Vector3.up * lTall * 0.82f, Quaternion.identity, new Vector3(lTall * 0.2f, lTall * 0.02f, lTall * 0.2f)), Color.white);
                            }
                        }
                    }
                    Spawn(lParent, lOrigin, lPosts, lPostMaterial, "HighwayPosts");
                    Spawn(lParent, lOrigin, lArches, lArchMaterial, "HighwayArches");
                    Spawn(lParent, lOrigin, lStones, lStone, "HighwayLandmarks");
                }
            }
        }

        private static void Spawn(Transform pParent, Vector3 pOrigin, MeshData pData, Material pMaterial, string pName)
        {
            if (pData.Count == 0) return;
            GameObject lObject = new GameObject(pName);
            lObject.transform.SetParent(pParent, false);
            lObject.transform.position = pOrigin;
            Mesh lMesh = pData.ToMesh(pName);
            lObject.AddComponent<MeshFilter>().sharedMesh = lMesh;
            MeshRenderer lRenderer = lObject.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterial = pMaterial;
            lRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Mesh _box;

        private static Mesh Box
        {
            get
            {
                if (_box != null) return _box;
                MeshData lData = new MeshData();
                GateKit.Box(lData, Vector3.zero, Vector3.one, Color.white);
                _box = lData.ToMesh("HighwayBox");
                return _box;
            }
        }
    }
}
