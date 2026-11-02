using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Gameplay obstacles (smashable ice, crates, rocks, cars).
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
            Placeholder(lRoot, new Vector3(2f, 1.5f, 4.4f), pColor);
            return Finish(lRoot, BreakableKind.Car, 1.6f, 2.2f, pColor);
        }

        // Neutral box standing in for a vehicle until the realistic models are imported (never shipped).
        public static GameObject Placeholder(Transform pParent, Vector3 pSize, Color pColor)
        {
            return Part(PrimitiveType.Cube, pParent, new Vector3(0f, pSize.y * 0.5f, 0f), pSize, pColor, default(Vector3), 0f);
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
    }
}
