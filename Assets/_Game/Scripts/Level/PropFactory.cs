using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Primitive-built stylized props : obstacles and themed scenery.
    public static class PropFactory
    {
        public static GameObject Part(PrimitiveType pType, Transform pParent, Vector3 pPosition, Vector3 pScale, Color pColor, Vector3 pEuler = default(Vector3), float pOutline = 0.02f)
        {
            GameObject lObject = Mats.Primitive(pType, pParent, pPosition, pScale, Mats.Toon(pColor, null, pOutline));
            lObject.transform.localRotation = Quaternion.Euler(pEuler);
            return lObject;
        }

        public static GameObject GlowPart(PrimitiveType pType, Transform pParent, Vector3 pPosition, Vector3 pScale, Color pColor, Vector3 pEuler = default(Vector3))
        {
            GameObject lObject = Mats.Primitive(pType, pParent, pPosition, pScale, Mats.Glow(pColor, false, 0f, 0f, true));
            lObject.transform.localRotation = Quaternion.Euler(pEuler);
            return lObject;
        }

        private static Transform Root(string pName, Transform pParent, Vector3 pPosition, Quaternion pRotation)
        {
            Transform lRoot = new GameObject(pName).transform;
            lRoot.SetParent(pParent, false);
            lRoot.SetPositionAndRotation(pPosition, pRotation);
            return lRoot;
        }

        // ---------- Obstacles ----------

        public static Breakable Ice(Transform pParent, Vector3 pPosition, Quaternion pRotation, float pSize)
        {
            Transform lRoot = Root("Ice", pParent, pPosition, pRotation);
            Color lColor = new Color(0.55f, 0.95f, 1f);
            for (int i = 0; i < 3; i++)
            {
                float lHeight = pSize * Random.Range(1.4f, 2.6f);
                Vector3 lOffset = new Vector3(Random.Range(-0.6f, 0.6f), lHeight * 0.4f, Random.Range(-0.6f, 0.6f)) * (i == 0 ? 0f : 1f);
                Part(PrimitiveType.Cube, lRoot, lOffset, new Vector3(pSize * 0.7f, lHeight, pSize * 0.7f), Color.Lerp(lColor, Color.white, i * 0.25f),
                    new Vector3(Random.Range(-18f, 18f), Random.Range(0f, 90f), Random.Range(-18f, 18f)), 0.03f);
            }
            return Finish(lRoot, BreakableKind.Ice, pSize * 1.1f, pSize * 2.4f, lColor);
        }

        public static Breakable Crate(Transform pParent, Vector3 pPosition, Quaternion pRotation, Color pColor)
        {
            Transform lRoot = Root("Crate", pParent, pPosition, pRotation);
            Part(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.4f, 0f), Vector3.one * 2.8f, pColor);
            Part(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.4f, 0f), new Vector3(2.9f, 0.5f, 2.9f), pColor * 0.7f, default(Vector3), 0f);
            Part(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.4f, 0f), new Vector3(0.5f, 2.9f, 2.9f), pColor * 0.7f, default(Vector3), 0f);
            return Finish(lRoot, BreakableKind.Crate, 1.8f, 2.8f, pColor);
        }

        public static Breakable Rock(Transform pParent, Vector3 pPosition, Quaternion pRotation, Color pColor, float pSize)
        {
            Transform lRoot = Root("Rock", pParent, pPosition, pRotation);
            Part(PrimitiveType.Sphere, lRoot, new Vector3(0f, pSize * 0.6f, 0f), new Vector3(pSize * 1.4f, pSize * 1.2f, pSize * 1.3f), pColor, new Vector3(20f, 30f, 10f), 0.03f);
            Part(PrimitiveType.Cube, lRoot, new Vector3(pSize * 0.3f, pSize * 0.9f, 0f), Vector3.one * pSize * 0.8f, pColor * 0.85f, new Vector3(35f, 20f, 45f), 0.03f);
            GlowPart(PrimitiveType.Sphere, lRoot, new Vector3(0f, pSize * 0.6f, 0f), Vector3.one * pSize * 0.5f, new Color(3f, 0.9f, 0.2f));
            return Finish(lRoot, BreakableKind.Rock, pSize * 0.9f, pSize * 1.6f, pColor);
        }

        public static Breakable Car(Transform pParent, Vector3 pPosition, Quaternion pRotation, Color pColor)
        {
            Transform lRoot = Root("Car", pParent, pPosition, pRotation);
            BuildCarBody(lRoot, pColor, false);
            return Finish(lRoot, BreakableKind.Car, 1.6f, 2.2f, pColor);
        }

        public static void BuildCarBody(Transform pRoot, Color pColor, bool pSport)
        {
            Color lDark = new Color(0.08f, 0.07f, 0.1f);
            float lLength = pSport ? 5f : 4.2f;
            Part(PrimitiveType.Cube, pRoot, new Vector3(0f, 0.75f, 0f), new Vector3(2.1f, pSport ? 0.7f : 0.9f, lLength), pColor);
            Part(PrimitiveType.Cube, pRoot, new Vector3(0f, pSport ? 1.3f : 1.55f, -0.3f), new Vector3(1.8f, pSport ? 0.5f : 0.75f, lLength * 0.5f), lDark);
            for (int i = 0; i < 4; i++)
            {
                float lX = i % 2 == 0 ? -1.05f : 1.05f;
                float lZ = i < 2 ? lLength * 0.32f : -lLength * 0.32f;
                Part(PrimitiveType.Cylinder, pRoot, new Vector3(lX, 0.45f, lZ), new Vector3(0.9f, 0.2f, 0.9f), lDark, new Vector3(0f, 0f, 90f));
            }
            GlowPart(PrimitiveType.Cube, pRoot, new Vector3(-0.7f, 0.8f, -lLength * 0.5f), new Vector3(0.5f, 0.25f, 0.1f), new Color(3f, 0.2f, 0.2f));
            GlowPart(PrimitiveType.Cube, pRoot, new Vector3(0.7f, 0.8f, -lLength * 0.5f), new Vector3(0.5f, 0.25f, 0.1f), new Color(3f, 0.2f, 0.2f));
            GlowPart(PrimitiveType.Cube, pRoot, new Vector3(-0.7f, 0.8f, lLength * 0.5f), new Vector3(0.5f, 0.25f, 0.1f), new Color(3f, 3f, 2f));
            GlowPart(PrimitiveType.Cube, pRoot, new Vector3(0.7f, 0.8f, lLength * 0.5f), new Vector3(0.5f, 0.25f, 0.1f), new Color(3f, 3f, 2f));
            if (pSport) Part(PrimitiveType.Cube, pRoot, new Vector3(0f, 1.4f, -lLength * 0.48f), new Vector3(2.2f, 0.12f, 0.6f), lDark);
        }

        private static Breakable Finish(Transform pRoot, BreakableKind pKind, float pRadius, float pHeight, Color pColor)
        {
            Breakable lBreakable = pRoot.gameObject.AddComponent<Breakable>();
            lBreakable.kind = pKind;
            lBreakable.radius = pRadius;
            lBreakable.height = pHeight;
            lBreakable.color = pColor;
            lBreakable.CacheRenderers();
            return lBreakable;
        }

        // ---------- Scenery ----------

        public static void Decor(DecorKind pKind, Theme pTheme, Transform pParent, Vector3 pPosition, Quaternion pRotation, float pScale)
        {
            Transform lRoot = Root("Decor", pParent, pPosition, pRotation);
            lRoot.localScale = Vector3.one * pScale;
            int lVariant = Random.Range(0, 3);
            switch (pKind)
            {
                case DecorKind.Islands: Island(lRoot, pTheme, lVariant); break;
                case DecorKind.City: Building(lRoot, pTheme, lVariant); break;
                case DecorKind.Desert: DesertProp(lRoot, pTheme, lVariant); break;
                case DecorKind.Industrial: Factory(lRoot, pTheme, lVariant); break;
                case DecorKind.Ice: Crystal(lRoot, pTheme, lVariant); break;
                case DecorKind.Space: SpaceProp(lRoot, pTheme, lVariant); break;
                case DecorKind.Void: VoidProp(lRoot, pTheme, lVariant); break;
                case DecorKind.Night: NightProp(lRoot, pTheme, lVariant); break;
            }
        }

        private static void Island(Transform pRoot, Theme pTheme, int pVariant)
        {
            // Floating mesa with striped strata and a lollipop tree.
            float lWidth = Random.Range(8f, 16f);
            float lHeight = Random.Range(10f, 30f);
            for (int i = 0; i < 4; i++)
            {
                float lShrink = 1f - i * 0.18f;
                Color lColor = i % 2 == 0 ? pTheme.decorB : pTheme.decorC;
                Part(PrimitiveType.Cube, pRoot, new Vector3(0f, -i * lHeight * 0.25f - lHeight * 0.125f, 0f), new Vector3(lWidth * lShrink, lHeight * 0.25f, lWidth * lShrink), lColor);
            }
            Part(PrimitiveType.Cube, pRoot, new Vector3(0f, 0.3f, 0f), new Vector3(lWidth * 1.02f, 0.8f, lWidth * 1.02f), pTheme.roadA);
            if (pVariant != 2)
            {
                float lTrunk = Random.Range(8f, 16f);
                Part(PrimitiveType.Cylinder, pRoot, new Vector3(0f, lTrunk * 0.5f, 0f), new Vector3(0.8f, lTrunk * 0.5f, 0.8f), new Color(0.95f, 0.92f, 0.85f));
                for (int i = 0; i < 3; i++)
                {
                    Vector3 lOffset = new Vector3(Random.Range(-2.5f, 2.5f), lTrunk + Random.Range(-1f, 1.5f), Random.Range(-2.5f, 2.5f));
                    Part(PrimitiveType.Sphere, pRoot, lOffset, new Vector3(6f, 3.2f, 6f) * Random.Range(0.8f, 1.2f), Color.Lerp(pTheme.decorA, pTheme.accent, Random.value * 0.5f));
                }
            }
            else
            {
                Part(PrimitiveType.Cube, pRoot, new Vector3(0f, 5f, 0f), new Vector3(lWidth * 0.5f, 10f, lWidth * 0.5f), pTheme.decorB, new Vector3(0f, 25f, 0f));
            }
        }

        private static void Building(Transform pRoot, Theme pTheme, int pVariant)
        {
            float lWidth = Random.Range(12f, 28f);
            float lHeight = Random.Range(30f, 110f);
            Part(PrimitiveType.Cube, pRoot, new Vector3(0f, lHeight * 0.5f - 40f, 0f), new Vector3(lWidth, lHeight, lWidth * Random.Range(0.7f, 1.2f)), pTheme.decorA);
            Color lWindow = pVariant == 0 ? pTheme.decorB * 1.6f : pTheme.decorC * 1.8f;
            for (float lY = -35f; lY < lHeight - 45f; lY += 7f)
                GlowPart(PrimitiveType.Cube, pRoot, new Vector3(0f, lY, 0f), new Vector3(lWidth * 1.01f, 1.2f, lWidth * 0.5f), lWindow);
            if (pVariant == 1)
            {
                Transform lBoard = new GameObject("Billboard").transform;
                lBoard.SetParent(pRoot, false);
                lBoard.localPosition = new Vector3(0f, lHeight - 30f, 0f);
                Part(PrimitiveType.Cube, lBoard, Vector3.zero, new Vector3(lWidth * 1.3f, 9f, 1f), pTheme.accent);
                Part(PrimitiveType.Cube, lBoard, new Vector3(0f, 0f, -0.6f), new Vector3(lWidth * 1.1f, 6f, 0.3f), Color.white, default(Vector3), 0f);
                GlowPart(PrimitiveType.Cube, lBoard, new Vector3(-lWidth * 0.2f, 0f, -0.8f), new Vector3(lWidth * 0.4f, 1.5f, 0.2f), pTheme.decorB * 2f);
            }
            else if (pVariant == 2)
            {
                Part(PrimitiveType.Cylinder, pRoot, new Vector3(0f, lHeight - 38f, 0f), new Vector3(lWidth * 0.6f, 4f, lWidth * 0.6f), pTheme.decorC);
                GlowPart(PrimitiveType.Cylinder, pRoot, new Vector3(0f, lHeight - 34f, 0f), new Vector3(lWidth * 0.5f, 0.6f, lWidth * 0.5f), pTheme.decorB * 2f);
            }
        }

        private static void DesertProp(Transform pRoot, Theme pTheme, int pVariant)
        {
            if (pVariant == 0 || pVariant == 1)
            {
                // Saguaro cactus
                float lHeight = Random.Range(10f, 22f);
                Part(PrimitiveType.Capsule, pRoot, new Vector3(0f, lHeight * 0.5f, 0f), new Vector3(2.6f, lHeight * 0.5f, 2.6f), pTheme.decorA);
                for (int i = -1; i <= 1; i += 2)
                {
                    float lArmY = lHeight * Random.Range(0.35f, 0.6f);
                    Part(PrimitiveType.Capsule, pRoot, new Vector3(i * 2.4f, lArmY, 0f), new Vector3(1.6f, 1.6f, 1.6f), pTheme.decorA, new Vector3(0f, 0f, 90f));
                    Part(PrimitiveType.Capsule, pRoot, new Vector3(i * 3.6f, lArmY + 2.6f, 0f), new Vector3(1.6f, 3f, 1.6f), pTheme.decorA);
                }
            }
            else
            {
                Part(PrimitiveType.Cube, pRoot, new Vector3(0f, 2f, 0f), new Vector3(9f, 14f, 6f), pTheme.decorB, new Vector3(0f, 30f, -25f));
            }
            Part(PrimitiveType.Sphere, pRoot, new Vector3(0f, -6f, 0f), new Vector3(30f, 14f, 30f), pTheme.decorC, default(Vector3), 0f);
        }

        private static void Factory(Transform pRoot, Theme pTheme, int pVariant)
        {
            float lHeight = Random.Range(25f, 70f);
            if (pVariant == 0)
            {
                Part(PrimitiveType.Cylinder, pRoot, new Vector3(0f, lHeight * 0.5f - 30f, 0f), new Vector3(6f, lHeight * 0.5f, 6f), pTheme.decorA);
                GlowPart(PrimitiveType.Cylinder, pRoot, new Vector3(0f, lHeight - 30f, 0f), new Vector3(6.2f, 0.8f, 6.2f), pTheme.accent * 2f);
            }
            else if (pVariant == 1)
            {
                Part(PrimitiveType.Cube, pRoot, new Vector3(0f, lHeight * 0.3f - 30f, 0f), new Vector3(24f, lHeight * 0.6f, 18f), pTheme.decorC);
                for (int i = -1; i <= 1; i++)
                    Part(PrimitiveType.Cylinder, pRoot, new Vector3(i * 7f, lHeight * 0.6f - 25f, 0f), new Vector3(3f, 10f, 3f), pTheme.decorA);
                GlowPart(PrimitiveType.Cube, pRoot, new Vector3(0f, -10f, -9.1f), new Vector3(20f, 3f, 0.3f), pTheme.decorB * 2.2f);
            }
            else
            {
                Part(PrimitiveType.Cylinder, pRoot, new Vector3(0f, 0f, 0f), new Vector3(3f, 40f, 3f), pTheme.decorB, new Vector3(0f, 0f, Random.Range(50f, 90f)));
                Part(PrimitiveType.Sphere, pRoot, new Vector3(0f, -10f, 0f), new Vector3(16f, 16f, 16f), pTheme.decorA);
            }
        }

        private static void Crystal(Transform pRoot, Theme pTheme, int pVariant)
        {
            int lCount = Random.Range(2, 5);
            for (int i = 0; i < lCount; i++)
            {
                float lHeight = Random.Range(14f, 45f);
                Vector3 lOffset = new Vector3(Random.Range(-6f, 6f), lHeight * 0.3f - 10f, Random.Range(-6f, 6f));
                Part(PrimitiveType.Cube, pRoot, lOffset, new Vector3(lHeight * 0.22f, lHeight, lHeight * 0.22f), i % 2 == 0 ? pTheme.decorA : pTheme.decorB,
                    new Vector3(Random.Range(-25f, 25f), 45f, Random.Range(-25f, 25f)), 0.03f);
            }
            Part(PrimitiveType.Sphere, pRoot, new Vector3(0f, -14f, 0f), new Vector3(26f, 12f, 26f), Color.white, default(Vector3), 0f);
        }

        private static void SpaceProp(Transform pRoot, Theme pTheme, int pVariant)
        {
            if (pVariant == 0)
            {
                float lSize = Random.Range(6f, 20f);
                Part(PrimitiveType.Sphere, pRoot, Vector3.zero, new Vector3(lSize, lSize * 0.8f, lSize * 0.9f), pTheme.decorA, new Vector3(Random.Range(0f, 90f), Random.Range(0f, 90f), 0f), 0.03f);
                GlowPart(PrimitiveType.Cube, pRoot, Vector3.zero, new Vector3(lSize * 1.05f, 0.4f, 0.4f), pTheme.decorB * 2f, new Vector3(0f, 0f, 30f));
            }
            else if (pVariant == 1)
            {
                // Arch ring like the blue tunnel reference.
                for (int i = 0; i < 10; i++)
                {
                    float lAngle = i * 18f;
                    Vector3 lDir = Quaternion.Euler(0f, 0f, lAngle) * Vector3.right;
                    GameObject lSeg = Part(PrimitiveType.Cube, pRoot, lDir * 22f, new Vector3(4f, 8f, 4f), i % 2 == 0 ? pTheme.decorA : pTheme.decorB);
                    lSeg.transform.localRotation = Quaternion.Euler(0f, 0f, lAngle);
                }
            }
            else
            {
                Part(PrimitiveType.Cylinder, pRoot, Vector3.zero, new Vector3(4f, 30f, 4f), pTheme.decorA, new Vector3(Random.Range(0f, 90f), 0f, Random.Range(40f, 90f)));
                GlowPart(PrimitiveType.Cylinder, pRoot, Vector3.zero, new Vector3(4.3f, 3f, 4.3f), pTheme.decorC * 2f, new Vector3(0f, 0f, 70f));
            }
        }

        private static void VoidProp(Transform pRoot, Theme pTheme, int pVariant)
        {
            float lSize = Random.Range(5f, 18f);
            Color lColor = pVariant == 0 ? pTheme.decorA : (pVariant == 1 ? pTheme.decorB : pTheme.decorC);
            Part(PrimitiveType.Cube, pRoot, Vector3.zero, new Vector3(lSize, lSize * Random.Range(1f, 4f), lSize), lColor,
                new Vector3(Random.Range(0f, 45f), Random.Range(0f, 90f), Random.Range(0f, 45f)), 0.04f);
        }

        private static void NightProp(Transform pRoot, Theme pTheme, int pVariant)
        {
            if (pVariant == 0)
            {
                // Dead tree
                Part(PrimitiveType.Cylinder, pRoot, new Vector3(0f, 8f, 0f), new Vector3(1.4f, 8f, 1.4f), pTheme.decorA);
                for (int i = 0; i < 4; i++)
                    Part(PrimitiveType.Cylinder, pRoot, new Vector3(0f, 10f + i * 2f, 0f), new Vector3(0.6f, 4f, 0.6f), pTheme.decorA, new Vector3(Random.Range(30f, 70f), i * 90f, 0f));
            }
            else if (pVariant == 1)
            {
                // Tombstones
                for (int i = 0; i < 3; i++)
                    Part(PrimitiveType.Cube, pRoot, new Vector3(i * 4f - 4f, 2f, Random.Range(-2f, 2f)), new Vector3(2.4f, 4f, 0.8f), pTheme.decorC, new Vector3(0f, 0f, Random.Range(-12f, 12f)));
            }
            else
            {
                Part(PrimitiveType.Cylinder, pRoot, new Vector3(0f, 6f, 0f), new Vector3(0.5f, 6f, 0.5f), pTheme.decorA);
                GlowPart(PrimitiveType.Sphere, pRoot, new Vector3(0f, 12.5f, 0f), Vector3.one * 1.6f, pTheme.decorB * 2.5f);
            }
            Part(PrimitiveType.Cube, pRoot, new Vector3(0f, -10f, 0f), new Vector3(22f, 20f, 22f), new Color(0.12f, 0.08f, 0.12f), new Vector3(0f, 45f, 0f), 0f);
        }
    }
}
