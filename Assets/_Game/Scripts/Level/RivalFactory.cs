using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum RivalKind { Moto, Bus, SportsCar, RaceCar, Train, Birds, Plane, FighterJet, Bullet, Rockets, Spaceship, Light, Time, TRex, Death, Supernova }

    // Rival identities (names, sounds, models) and how they sit in the world. Models are real assets wired in GameAssets,
    // scaled up because everything here is enormous. Light, Time, Death and the Supernova are effects, built in code.
    public static class RivalFactory
    {
        private const float WORLD_SCALE = 2.6f;
        // The world grew six times wider : every rival grows too, so it still reads from far back.
        public const float GROW = 2.4f;

        public struct Placement
        {
            public bool flying;
            public bool fromSky;
            public float lateral;
            public float height;
            public float bob;
        }

        public static Placement GetPlacement(RivalKind pKind)
        {
            switch (pKind)
            {
                case RivalKind.Moto: return new Placement { lateral = 22f };
                case RivalKind.Bus: return new Placement { lateral = -30f };
                case RivalKind.SportsCar: return new Placement { lateral = -24f };
                case RivalKind.RaceCar: return new Placement { lateral = 24f };
                case RivalKind.Train: return new Placement { lateral = -55f };
                case RivalKind.Birds: return new Placement { flying = true, lateral = 24f, height = 16f, bob = 3f };
                case RivalKind.Plane: return new Placement { flying = true, fromSky = true, lateral = 40f, height = 70f, bob = 3f };
                case RivalKind.FighterJet: return new Placement { flying = true, fromSky = true, lateral = -30f, height = 22f, bob = 2f };
                case RivalKind.Bullet: return new Placement { flying = true, lateral = 16f, height = 8f, bob = 0.2f };
                case RivalKind.Rockets: return new Placement { flying = true, fromSky = true, lateral = 0f, height = 14f, bob = 2f };
                case RivalKind.Spaceship: return new Placement { flying = true, fromSky = true, lateral = -26f, height = 12f, bob = 3f };
                case RivalKind.Light: return new Placement { flying = true, lateral = 14f, height = 8f, bob = 0.5f };
                case RivalKind.Time: return new Placement { flying = true, fromSky = true, lateral = 0f, height = 40f, bob = 1f };
                case RivalKind.TRex: return new Placement { lateral = 0f };
                case RivalKind.Death: return new Placement { flying = true, lateral = 0f, height = 12f, bob = 2f };
                case RivalKind.Supernova: return new Placement { flying = true, lateral = 0f, height = 0f };
            }
            return new Placement();
        }

        public static string EngineSound(RivalKind pKind)
        {
            switch (pKind)
            {
                case RivalKind.Moto:
                case RivalKind.SportsCar:
                case RivalKind.RaceCar: return "sfx_engine_car";
                case RivalKind.Bus:
                case RivalKind.Train: return "sfx_engine_heavy";
                case RivalKind.Plane:
                case RivalKind.FighterJet:
                case RivalKind.Rockets:
                case RivalKind.Spaceship: return "sfx_engine_jet";
                case RivalKind.Time: return "sfx_engine_tick";
                case RivalKind.TRex:
                case RivalKind.Death:
                case RivalKind.Supernova: return "sfx_engine_dread";
                default: return "sfx_engine_hum";
            }
        }

        public static string DisplayName(RivalKind pKind)
        {
            switch (pKind)
            {
                case RivalKind.Moto: return "THE MOTORBIKE";
                case RivalKind.Bus: return "THE CITY BUS";
                case RivalKind.SportsCar: return "THE SPORTS CAR";
                case RivalKind.RaceCar: return "THE RACE CAR";
                case RivalKind.Train: return "THE BULLET TRAIN";
                case RivalKind.Birds: return "THE FALCONS";
                case RivalKind.Plane: return "THE AIRLINER";
                case RivalKind.FighterJet: return "THE FIGHTER JET";
                case RivalKind.Bullet: return "A SNIPER BULLET";
                case RivalKind.Rockets: return "THE ROCKETS";
                case RivalKind.Spaceship: return "THE STARFIGHTER";
                case RivalKind.Light: return "LIGHT ITSELF";
                case RivalKind.Time: return "TIME";
                case RivalKind.TRex: return "THE T-REX";
                case RivalKind.Death: return "DEATH";
                case RivalKind.Supernova: return "A SUPERNOVA";
            }
            return "???";
        }

        // Rough real-world footprint, used when a model is missing.
        public static Vector3 Size(RivalKind pKind)
        {
            switch (pKind)
            {
                case RivalKind.Moto: return new Vector3(0.8f, 1.4f, 2.2f);
                case RivalKind.Bus: return new Vector3(2.6f, 3.2f, 12f);
                case RivalKind.SportsCar: return new Vector3(2f, 1.2f, 4.6f);
                case RivalKind.RaceCar: return new Vector3(2f, 1f, 5.6f);
                case RivalKind.Train: return new Vector3(3.4f, 4f, 50f);
                case RivalKind.Birds: return new Vector3(6f, 1f, 2f);
                case RivalKind.Plane: return new Vector3(36f, 12f, 38f);
                case RivalKind.FighterJet: return new Vector3(10f, 5f, 16f);
                case RivalKind.Bullet: return new Vector3(0.6f, 0.6f, 2f);
                case RivalKind.Rockets: return new Vector3(8f, 2f, 7f);
                case RivalKind.Spaceship: return new Vector3(12f, 3f, 14f);
                case RivalKind.TRex: return new Vector3(3f, 6f, 12f);
            }
            return Vector3.one * 2f;
        }

        private static float ModelScale(RivalKind pKind)
        {
            // Small and bright : the rival is a glowing mark on the track, not a building. The airliner is the exception,
            // an immense thing flying over the whole stage.
            switch (pKind)
            {
                case RivalKind.Bullet: return 8f * GROW * 0.5f;
                case RivalKind.Moto: return 4f * GROW * 0.5f;
                case RivalKind.TRex: return 4.5f * GROW;
                case RivalKind.Birds: return 5f * GROW * 0.5f;
                case RivalKind.Plane: return WORLD_SCALE * GROW * 3f;
                default: return WORLD_SCALE * GROW * 0.5f;
            }
        }

        public static Transform Build(RivalKind pKind, Transform pParent)
        {
            Transform lRoot = new GameObject("Rival_" + pKind).transform;
            lRoot.SetParent(pParent, false);
            switch (pKind)
            {
                case RivalKind.Light: Glow(lRoot, new Color(4f, 4f, 3.6f), new Vector3(3f, 3f, 34f) * GROW); return lRoot;
                case RivalKind.Time: BuildClock(lRoot); lRoot.localScale = Vector3.one * GROW; return lRoot;
                case RivalKind.Death: BuildReaper(lRoot); lRoot.localScale = Vector3.one * GROW; return lRoot;
                case RivalKind.Supernova: BuildStar(lRoot); return lRoot;
            }
            GameObject lPrefab = Model(pKind);
            if (pKind == RivalKind.Birds)
            {
                Vector3[] lFlock =
                {
                    Vector3.zero, new Vector3(-9f, 2f, -8f), new Vector3(9f, -1f, -9f), new Vector3(-17f, 4f, -18f), new Vector3(16f, 1f, -19f)
                };
                foreach (Vector3 lOffset in lFlock) Instance(lPrefab, pKind, lRoot, lOffset * 2f * GROW);
                return lRoot;
            }
            Instance(lPrefab, pKind, lRoot, Vector3.zero);
            return lRoot;
        }

        private static GameObject Model(RivalKind pKind)
        {
            GameObject[] lModels = GameAssets.Instance.rivalModels;
            int lIndex = (int)pKind;
            return lModels != null && lIndex < lModels.Length ? lModels[lIndex] : null;
        }

        private static void Instance(GameObject pPrefab, RivalKind pKind, Transform pRoot, Vector3 pOffset)
        {
            float lScale = ModelScale(pKind);
            if (pPrefab == null)
            {
                Vector3 lSize = Size(pKind) * lScale;
                MeshData lData = new MeshData();
                GateKit.Box(lData, pOffset + new Vector3(0f, lSize.y * 0.5f, 0f), lSize, new Color(0.9f, 0.3f, 0.25f));
                GameObject lBox = new GameObject("Missing_" + pKind);
                lBox.transform.SetParent(pRoot, false);
                lBox.AddComponent<MeshFilter>().sharedMesh = lData.ToMesh("Missing");
                lBox.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(new Color(0.6f, 0.55f, 0.8f), false);
                return;
            }
            GameObject lObject = Object.Instantiate(pPrefab, pRoot);
            lObject.transform.localPosition = pOffset;
            lObject.transform.localRotation = Quaternion.identity;
            lObject.transform.localScale = Vector3.one * lScale;
            // Toon with a strong rim : the rival's silhouette catches the light and reads against any ground.
            Material lMaterial = Mats.Toon(new Color(0.95f, 0.9f, 1f), new Color(0.55f, 0.48f, 0.78f), 0f, 0.75f);
            foreach (Renderer lRenderer in lObject.GetComponentsInChildren<Renderer>())
            {
                Material[] lMaterials = new Material[lRenderer.sharedMaterials.Length];
                for (int i = 0; i < lMaterials.Length; i++) lMaterials[i] = lMaterial;
                lRenderer.sharedMaterials = lMaterials;
            }
            foreach (Collider lCollider in lObject.GetComponentsInChildren<Collider>()) Object.Destroy(lCollider);
        }

        private static GameObject Glow(Transform pParent, Color pColor, Vector3 pScale)
        {
            return Mats.Primitive(PrimitiveType.Sphere, pParent, Vector3.zero, pScale, Mats.Glow(pColor, false, 0f, 0f, true));
        }

        // A clock the size of a building, hands racing.
        private static void BuildClock(Transform pRoot)
        {
            Mats.Primitive(PrimitiveType.Cylinder, pRoot, Vector3.zero, new Vector3(60f, 2f, 60f), Mats.Flat(new Color(0.7f, 0.7f, 0.8f), false, new Color(0.95f, 0.92f, 0.8f))).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject lMinute = new GameObject("Hand");
            lMinute.transform.SetParent(pRoot, false);
            lMinute.transform.localPosition = new Vector3(0f, 0f, -3f);
            Mats.Primitive(PrimitiveType.Cube, lMinute.transform, new Vector3(0f, 12f, 0f), new Vector3(2.5f, 24f, 1f), Mats.Flat(new Color(0.3f, 0.3f, 0.4f), false, new Color(0.08f, 0.06f, 0.12f)));
            lMinute.AddComponent<Spinner>().speed = 720f;
            GameObject lHour = new GameObject("Hand");
            lHour.transform.SetParent(pRoot, false);
            lHour.transform.localPosition = new Vector3(0f, 0f, -3.5f);
            Mats.Primitive(PrimitiveType.Cube, lHour.transform, new Vector3(0f, 8f, 0f), new Vector3(4f, 16f, 1f), Mats.Flat(new Color(0.3f, 0.3f, 0.4f), false, new Color(0.9f, 0.1f, 0.15f)));
            lHour.AddComponent<Spinner>().speed = 90f;
        }

        private static void BuildReaper(Transform pRoot)
        {
            GameObject lRobe = new GameObject("Robe");
            lRobe.transform.SetParent(pRoot, false);
            lRobe.transform.localScale = new Vector3(12f, 26f, 12f);
            lRobe.AddComponent<MeshFilter>().sharedMesh = RockSculptor.Shard(1);
            lRobe.AddComponent<MeshRenderer>().sharedMaterial = Mats.Flat(new Color(0.4f, 0.3f, 0.5f), false, new Color(0.05f, 0.03f, 0.06f));
            Glow(pRoot, new Color(4f, 0.3f, 0.3f), new Vector3(3f, 3f, 3f)).transform.localPosition = new Vector3(-2f, 22f, -6f);
            Glow(pRoot, new Color(4f, 0.3f, 0.3f), new Vector3(3f, 3f, 3f)).transform.localPosition = new Vector3(2f, 22f, -6f);
        }

        private static void BuildStar(Transform pRoot)
        {
            Glow(pRoot, new Color(5f, 3.6f, 1.6f), Vector3.one * 180f);
            Mats.Primitive(PrimitiveType.Sphere, pRoot, Vector3.zero, Vector3.one * 240f, Mats.Glow(new Color(2.5f, 0.7f, 0.2f, 0.35f), true));
        }
    }

    public class Spinner : MonoBehaviour
    {
        public float speed = 90f;

        private void Update() => transform.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self);
    }
}
