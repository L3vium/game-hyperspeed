using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Spawns the gameplay props of a layout once deck frames are final : pads, rings, rails, gates, obstacles, debris.
    public static class LevelProps
    {
        private static Quaternion Rotation(Deck pDeck, float pS)
        {
            Vector3 lC, lF, lR, lU;
            float lW;
            pDeck.Frame(pS, out lC, out lF, out lR, out lU, out lW);
            return Quaternion.LookRotation(lF, lU);
        }

        private static Vector3 Up(Deck pDeck, float pS)
        {
            Vector3 lC, lF, lR, lU;
            float lW;
            pDeck.Frame(pS, out lC, out lF, out lR, out lU, out lW);
            return lU;
        }

        private static GameObject MeshObject(string pName, Transform pParent, MeshData pData, Material pMaterial, int pLayer = Layers.PROP)
        {
            GameObject lObject = new GameObject(pName);
            lObject.transform.SetParent(pParent, false);
            lObject.layer = pLayer;
            lObject.AddComponent<MeshFilter>().sharedMesh = pData.ToMesh(pName);
            lObject.AddComponent<MeshRenderer>().sharedMaterial = pMaterial;
            return lObject;
        }

        // ---------------------------------------------------------------- pads, rings, obstacles

        public static void Boost(Transform pRoot, Deck pDeck, float pS, float pLateral, Theme pTheme)
        {
            BoostPad.Create(pRoot, pDeck.SurfacePoint(pS, pLateral), Rotation(pDeck, pS), pTheme.accent);
        }

        public static void Jump(Transform pRoot, Deck pFrom, float pS, float pLateral, Deck pTo, float pToS, float pToLateral, Theme pTheme)
        {
            JumpPad.Create(pRoot, pFrom.SurfacePoint(pS, pLateral), Rotation(pFrom, pS), pTo.SurfacePoint(pToS, pToLateral), Up(pTo, pToS), UIKit.GOLD);
        }

        public static void Ring(Transform pRoot, LevelLayout pLayout, float pS, float pLateral, float pHeight, Theme pTheme)
        {
            Vector3 lForward = pLayout.spine.Forward(pS);
            Quaternion lRotation = Quaternion.LookRotation((lForward - Vector3.up * 0.55f).normalized, Vector3.up);
            BoostRing.Create(pRoot, pLayout.spine.Point(pS, pLateral, pHeight), lRotation, pTheme.accent, 5f);
        }

        public static void Obstacle(Transform pRoot, Deck pDeck, float pS, float pLateral, float pSize, Theme pTheme)
        {
            Vector3 lPosition = pDeck.SurfacePoint(pS, pLateral);
            Quaternion lRotation = Rotation(pDeck, pS);
            switch (pTheme.decor)
            {
                case DecorKind.Ice:
                    PropFactory.Ice(pRoot, lPosition, lRotation, 1.3f * pSize);
                    break;
                case DecorKind.Islands:
                case DecorKind.City:
                    PropFactory.Crate(pRoot, lPosition, lRotation, Color.Lerp(pTheme.decorB, new Color(0.85f, 0.6f, 0.3f), 0.5f));
                    break;
                default:
                    PropFactory.Rock(pRoot, lPosition, lRotation, Color.Lerp(pTheme.decorC, pTheme.roadSide, 0.5f), 2.4f * pSize);
                    break;
            }
        }

        // ---------------------------------------------------------------- gates

        public static void StartGate(Transform pRoot, Deck pDeck, float pS, Theme pTheme)
        {
            Hyperspeed.StartGate.Create(pRoot, pDeck.SurfacePoint(pS, 0f), Rotation(pDeck, pS), pDeck.Width(pS), pS, pTheme);
        }

        public static void FinishGate(Transform pRoot, Deck pDeck, float pS, Theme pTheme)
        {
            Transform lGate = new GameObject("FinishGate").transform;
            lGate.SetParent(pRoot, false);
            lGate.SetPositionAndRotation(pDeck.SurfacePoint(pS, 0f), Rotation(pDeck, pS));
            Arch(lGate, pDeck.Width(pS) * 0.5f + 1.5f, 13f, pTheme, true);
        }

        // Two sculpted pillars, a beam with a glowing strip (and a checker banner for the finish).
        public static void Arch(Transform pParent, float pHalf, float pHeight, Theme pTheme, bool pChecker)
        {
            MeshData lData = new MeshData();
            Color lPillar = pChecker ? UIKit.RED : Color.Lerp(pTheme.roadSide, pTheme.decorC, 0.3f);
            for (int lSide = -1; lSide <= 1; lSide += 2)
                RockSculptor.Place(lData, RockSculptor.Shard(lSide + 2), new Vector3(lSide * pHalf, -1f, 0f), Quaternion.Euler(0f, lSide * 20f, 0f), new Vector3(2.6f, pHeight / 2.6f + 0.6f, 2.6f), lPillar);
            Vector3 lA = new Vector3(-pHalf, pHeight, -0.8f);
            Vector3 lB = new Vector3(pHalf, pHeight, -0.8f);
            Box(lData, lA, lB, 2.4f, 1.6f, Color.Lerp(pTheme.roadSide, Color.white, 0.2f));
            if (pChecker)
            {
                int lCount = Mathf.CeilToInt(pHalf * 2f / 1.5f);
                float lWidth = pHalf * 2f / lCount;
                for (int i = 0; i < lCount; i++)
                    for (int j = 0; j < 2; j++)
                    {
                        float lX = -pHalf + i * lWidth;
                        float lY = pHeight - 2.6f - j * 1.3f;
                        Color lColor = (i + j) % 2 == 0 ? Color.white : UIKit.INK;
                        lData.Quad(new Vector3(lX, lY, -0.85f), new Vector3(lX + lWidth, lY, -0.85f), new Vector3(lX + lWidth, lY + 1.3f, -0.85f), new Vector3(lX, lY + 1.3f, -0.85f), Vector3.back, lColor);
                        lData.Quad(new Vector3(lX, lY, -0.75f), new Vector3(lX + lWidth, lY, -0.75f), new Vector3(lX + lWidth, lY + 1.3f, -0.75f), new Vector3(lX, lY + 1.3f, -0.75f), Vector3.forward, lColor);
                    }
            }
            MeshObject("Arch", pParent, lData, Mats.Toon(Color.white, pTheme.shade, 0.02f, 0.3f, true));
            MeshData lStrip = new MeshData();
            Box(lStrip, lA + Vector3.down * 0.95f + Vector3.back * 0.9f, lB + Vector3.down * 0.95f + Vector3.back * 0.9f, 0.35f, 0.2f, Color.white);
            MeshObject("ArchGlow", pParent, lStrip, Mats.Glow((pChecker ? Color.white : pTheme.accent) * 2.5f, false, 0f, 0f, true));
        }

        private static void Box(MeshData pData, Vector3 pFrom, Vector3 pTo, float pHeight, float pDepth, Color pColor)
        {
            Vector3 lAxis = (pTo - pFrom).normalized;
            Vector3 lUp = Vector3.up * pHeight * 0.5f;
            Vector3 lBack = Vector3.Cross(lAxis, Vector3.up).normalized * pDepth * 0.5f;
            Vector3[] lC =
            {
                pFrom - lUp - lBack, pTo - lUp - lBack, pTo + lUp - lBack, pFrom + lUp - lBack,
                pFrom - lUp + lBack, pTo - lUp + lBack, pTo + lUp + lBack, pFrom + lUp + lBack
            };
            Vector3 lN = Vector3.Cross(lAxis, Vector3.up).normalized;
            pData.Quad(lC[0], lC[1], lC[2], lC[3], -lN, pColor);
            pData.Quad(lC[4], lC[5], lC[6], lC[7], lN, pColor);
            pData.Quad(lC[3], lC[2], lC[6], lC[7], Vector3.up, pColor);
            pData.Quad(lC[0], lC[1], lC[5], lC[4], Vector3.down, pColor * 0.7f);
            pData.Quad(lC[0], lC[3], lC[7], lC[4], -lAxis, pColor);
            pData.Quad(lC[1], lC[2], lC[6], lC[5], lAxis, pColor);
        }

        // Glowing chevrons on a ramp or an approach, pointing where you are going.
        public static void RampChevrons(Transform pRoot, Deck pDeck, float pS0, float pS1, Theme pTheme)
        {
            MeshData lData = new MeshData();
            for (float lS = pS0 + 4f; lS < pS1 - 2f; lS += 7f)
            {
                Vector3 lC, lF, lR, lU;
                float lW;
                pDeck.Frame(lS, out lC, out lF, out lR, out lU, out lW);
                float lHalf = Mathf.Min(lW * 0.32f, 5f);
                Vector3 lTip = pDeck.SurfacePoint(lS + 2.2f, 0f) + lU * 0.06f;
                for (int lSide = -1; lSide <= 1; lSide += 2)
                {
                    Vector3 lOuter = pDeck.SurfacePoint(lS - 1.4f, lSide * lHalf) + lU * 0.06f;
                    Vector3 lThick = lF * 1.1f;
                    lData.Quad(lOuter, lTip, lTip + lThick, lOuter + lThick, lU, Color.white);
                }
            }
            if (lData.Count > 0) MeshObject("Chevrons", pRoot, lData, Mats.Glow(pTheme.accent * 2.2f, false, 0f, 0f, true));
        }

        // ---------------------------------------------------------------- rails

        private static GrindRail NewRail(Transform pRoot, LevelLayout pLayout, bool pZip, string pName)
        {
            GrindRail lRail = new GameObject(pName).AddComponent<GrindRail>();
            lRail.transform.SetParent(pRoot, false);
            lRail.zip = pZip;
            pLayout.rails.Add(lRail);
            return lRail;
        }

        // Rail buried in the approach trough, over the gap, buried again in the landing trough.
        public static void BridgeRail(Transform pRoot, LevelLayout pLayout, Deck pFrom, Deck pTo, float pGap0, float pGap1, Theme pTheme)
        {
            GrindRail lRail = NewRail(pRoot, pLayout, false, "BridgeRail");
            Vector3 lLip = pFrom.SurfacePoint(pGap0, 0f) + Up(pFrom, pGap0) * 0.35f;
            Vector3 lLand = pTo.SurfacePoint(pGap1, 0f) + Up(pTo, pGap1) * 0.35f;
            float lSpan = Mathf.Max(1f, pGap1 - pGap0);
            for (float lS = pGap0 - 12f; lS <= pGap1 + 12.01f; lS += Spine.STEP)
            {
                Vector3 lPoint;
                Vector3 lUp = Vector3.up;
                if (lS <= pGap0) { lPoint = pFrom.SurfacePoint(lS, 0f) + Up(pFrom, lS) * 0.35f; lUp = Up(pFrom, lS); }
                else if (lS >= pGap1) { lPoint = pTo.SurfacePoint(lS, 0f) + Up(pTo, lS) * 0.35f; lUp = Up(pTo, lS); }
                else
                {
                    float lU = (lS - pGap0) / lSpan;
                    float lRise = 1.35f * Mathf.Clamp01(Mathf.Min(lU * lSpan, (1f - lU) * lSpan) / 10f);
                    float lY = Mathf.Lerp(lLip.y, lLand.y, LevelBuilder.Smooth(lU)) + lRise;
                    lPoint = pLayout.spine.Point(lS, 0f, lY);
                }
                lRail.AddPoint(lPoint, lUp, lS);
            }
            lRail.Finish(pTheme.accent);
        }

        public static void GutterRail(Transform pRoot, LevelLayout pLayout, Deck pDeck, float pS0, float pS1, int pSide, Theme pTheme)
        {
            GrindRail lRail = NewRail(pRoot, pLayout, false, "GutterRail");
            for (float lS = pS0; lS <= pS1 + 0.01f; lS += Spine.STEP)
            {
                float lLateral = pSide * (pDeck.Width(lS) * 0.5f - 1f);
                lRail.AddPoint(pDeck.SurfacePoint(lS, lLateral) + Up(pDeck, lS) * 0.35f, Up(pDeck, lS), lS);
            }
            lRail.Finish(pTheme.accent);
        }

        public static void WeaveRail(Transform pRoot, LevelLayout pLayout, float pS0, float pS1, float pHeight, Deck pLanding, float pPhase, Theme pTheme)
        {
            GrindRail lRail = NewRail(pRoot, pLayout, false, "WeaveRail");
            float lEndY = pLanding.SurfacePoint(pS1, 0f).y + 1.2f;
            for (float lS = pS0; lS <= pS1 + 0.01f; lS += Spine.STEP)
            {
                float lU = Mathf.InverseLerp(pS0, pS1, lS);
                float lLateral = 9f * Mathf.Sin(lU * Mathf.PI * 1.5f + pPhase) * Mathf.Sin(lU * Mathf.PI);
                float lY = Mathf.Lerp(pHeight, lEndY, LevelBuilder.Smooth(lU)) + Mathf.Sin(lU * Mathf.PI) * 2f;
                lRail.AddPoint(pLayout.spine.Point(lS, lLateral, lY), Vector3.up, lS);
            }
            lRail.Finish(pTheme.accent);
        }

        public static void Zip(Transform pRoot, LevelLayout pLayout, Deck pFrom, Deck pTo, float pS0, float pS1, Theme pTheme)
        {
            GrindRail lRail = NewRail(pRoot, pLayout, true, "ZipLine");
            Vector3 lStart = pFrom.SurfacePoint(pS0 - 12f, 0f) + Vector3.up * 4.4f;
            Vector3 lEnd = pTo.SurfacePoint(pS1 + 8f, 0f) + Vector3.up * 4.4f;
            float lS0 = pS0 - 12f, lS1 = pS1 + 8f;
            for (float lS = lS0; lS <= lS1 + 0.01f; lS += Spine.STEP * 2f)
            {
                float lU = Mathf.InverseLerp(lS0, lS1, lS);
                float lY = Mathf.Lerp(lStart.y, lEnd.y, lU) - Mathf.Sin(lU * Mathf.PI) * 1.2f;
                lRail.AddPoint(pLayout.spine.Point(lS, 0f, lY), Vector3.up, lS);
            }
            lRail.FinishZip(pTheme.roadSide, pTheme.accent);
        }

        // ---------------------------------------------------------------- scenery inside plunges

        // Floating rocks around a long fall (visual only, kept off the dive line).
        public static void DebrisField(Transform pRoot, LevelLayout pLayout, float pS0, float pS1, float pTop, float pBottom, Theme pTheme)
        {
            MeshData lData = new MeshData();
            RockKit.Batch lRocks = RockKit.Available ? new RockKit.Batch() : null;
            System.Random lRandom = new System.Random(Mathf.RoundToInt(pS0 * 13f));
            int lCount = Mathf.RoundToInt((pS1 - pS0) / 6f);
            for (int i = 0; i < lCount; i++)
            {
                float lS = Mathf.Lerp(pS0, pS1, (float)lRandom.NextDouble());
                float lSide = lRandom.NextDouble() < 0.5 ? -1f : 1f;
                float lLateral = lSide * (12f + (float)lRandom.NextDouble() * 45f);
                float lY = Mathf.Lerp(pBottom + 10f, pTop + 15f, (float)lRandom.NextDouble());
                float lSize = 2f + (float)lRandom.NextDouble() * 7f;
                Quaternion lRotation = Quaternion.Euler((float)lRandom.NextDouble() * 360f, (float)lRandom.NextDouble() * 360f, (float)lRandom.NextDouble() * 360f);
                Color lColor = Color.Lerp(pTheme.roadSide, pTheme.decorC, (float)lRandom.NextDouble());
                if (lRocks != null) lRocks.Boulder(lRandom.Next(0, RockKit.BOULDERS), true, pLayout.spine.Point(lS, lLateral, lY), lRotation, lSize * 1.3f, 1f, RockKit.Tint(lColor));
                else RockSculptor.Place(lData, RockSculptor.Boulder(lRandom.Next(0, 6)), pLayout.spine.Point(lS, lLateral, lY), lRotation, new Vector3(lSize, lSize * 0.8f, lSize), lColor);
            }
            if (lRocks != null && !lRocks.Empty) lRocks.Build("Debris", pRoot, Layers.DEBRIS, pTheme.shade, false);
            if (lData.Count > 0) MeshObject("Debris", pRoot, lData, Mats.Toon(Color.white, pTheme.shade, 0f, 0.3f, true), Layers.DEBRIS);
        }
    }
}
