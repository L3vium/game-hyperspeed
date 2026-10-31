using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum RivalKind { Moto, Bus, SportsCar, RaceCar, Train, Plane, FighterJet, Bullet, Rockets, Spaceship, Light, Time, Death, Supernova }

    // Builds the absurd rival models out of primitives, plus their placement relative to the track.
    public static class RivalFactory
    {
        public struct Placement
        {
            public float lateral;
            public float height;
            public float bob;
        }

        public static Placement GetPlacement(RivalKind pKind, float pTrackWidth)
        {
            float lSide = pTrackWidth * 0.5f + 6f;
            switch (pKind)
            {
                case RivalKind.Moto: return new Placement { lateral = -pTrackWidth * 0.3f, height = 0f };
                case RivalKind.Bus: return new Placement { lateral = lSide + 2f, height = 0f };
                case RivalKind.SportsCar: return new Placement { lateral = lSide, height = 0f };
                case RivalKind.RaceCar: return new Placement { lateral = lSide, height = 0f };
                case RivalKind.Train: return new Placement { lateral = lSide + 3f, height = -1f };
                case RivalKind.Plane: return new Placement { lateral = 10f, height = 22f, bob = 2f };
                case RivalKind.FighterJet: return new Placement { lateral = -8f, height = 16f, bob = 1.5f };
                case RivalKind.Bullet: return new Placement { lateral = 5f, height = 6f, bob = 0.2f };
                case RivalKind.Rockets: return new Placement { lateral = 0f, height = 14f, bob = 1.5f };
                case RivalKind.Spaceship: return new Placement { lateral = -9f, height = 12f, bob = 2f };
                case RivalKind.Light: return new Placement { lateral = 0f, height = 4f, bob = 0.5f };
                case RivalKind.Time: return new Placement { lateral = 0f, height = 26f, bob = 1f };
                case RivalKind.Death: return new Placement { lateral = 0f, height = 6f, bob = 1.5f };
                case RivalKind.Supernova: return new Placement { lateral = 0f, height = 0f };
            }
            return new Placement();
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
                case RivalKind.Plane: return "THE AIRLINER";
                case RivalKind.FighterJet: return "THE FIGHTER JET";
                case RivalKind.Bullet: return "A SNIPER BULLET";
                case RivalKind.Rockets: return "THE ROCKETS";
                case RivalKind.Spaceship: return "THE STARFIGHTER";
                case RivalKind.Light: return "LIGHT ITSELF";
                case RivalKind.Time: return "TIME";
                case RivalKind.Death: return "DEATH";
                case RivalKind.Supernova: return "A SUPERNOVA";
            }
            return "???";
        }

        private static GameObject P(PrimitiveType pType, Transform pParent, Vector3 pPos, Vector3 pScale, Color pColor, Vector3 pEuler = default(Vector3))
        {
            return PropFactory.Part(pType, pParent, pPos, pScale, pColor, pEuler, 0.03f);
        }

        private static GameObject G(PrimitiveType pType, Transform pParent, Vector3 pPos, Vector3 pScale, Color pColor, Vector3 pEuler = default(Vector3))
        {
            return PropFactory.GlowPart(pType, pParent, pPos, pScale, pColor, pEuler);
        }

        private static void Trail(Transform pParent, Vector3 pLocal, float pWidth, Color pColor, float pTime)
        {
            Transform lAnchor = new GameObject("Trail").transform;
            lAnchor.SetParent(pParent, false);
            lAnchor.localPosition = pLocal;
            TrailRenderer lTrail = lAnchor.gameObject.AddComponent<TrailRenderer>();
            lTrail.time = pTime;
            lTrail.startWidth = pWidth;
            lTrail.endWidth = 0f;
            lTrail.minVertexDistance = 0.5f;
            lTrail.sharedMaterial = Mats.Glow(pColor, true);
            lTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public static Transform Build(RivalKind pKind, Transform pParent)
        {
            Transform lRoot = new GameObject("Rival_" + pKind).transform;
            lRoot.SetParent(pParent, false);
            Color lDark = new Color(0.08f, 0.07f, 0.1f);
            Color lFlame = new Color(3f, 1.4f, 0.3f);

            switch (pKind)
            {
                case RivalKind.Moto:
                    P(PrimitiveType.Cylinder, lRoot, new Vector3(0f, 0.7f, 1.1f), new Vector3(1.4f, 0.2f, 1.4f), lDark, new Vector3(0f, 0f, 90f));
                    P(PrimitiveType.Cylinder, lRoot, new Vector3(0f, 0.7f, -1.1f), new Vector3(1.4f, 0.2f, 1.4f), lDark, new Vector3(0f, 0f, 90f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.2f, 0f), new Vector3(0.7f, 0.7f, 2.2f), new Color(1f, 0.15f, 0.2f));
                    P(PrimitiveType.Capsule, lRoot, new Vector3(0f, 2.1f, -0.2f), new Vector3(0.8f, 0.8f, 0.8f), new Color(0.15f, 0.15f, 0.2f), new Vector3(30f, 0f, 0f));
                    P(PrimitiveType.Sphere, lRoot, new Vector3(0f, 2.9f, 0.2f), Vector3.one * 0.8f, Color.white);
                    G(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.3f, 1.2f), new Vector3(0.4f, 0.3f, 0.1f), new Color(3f, 3f, 2f));
                    Trail(lRoot, new Vector3(0f, 0.8f, -1.6f), 0.8f, new Color(2f, 0.2f, 0.3f), 0.4f);
                    break;
                case RivalKind.Bus:
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 2.6f, 0f), new Vector3(3.4f, 3.8f, 12f), new Color(1f, 0.82f, 0.1f));
                    for (int i = 0; i < 5; i++)
                        P(PrimitiveType.Cube, lRoot, new Vector3(0f, 3.2f, -4.4f + i * 2.2f), new Vector3(3.5f, 1.2f, 1.6f), new Color(0.3f, 0.6f, 0.9f));
                    for (int i = 0; i < 4; i++)
                        P(PrimitiveType.Cylinder, lRoot, new Vector3(i % 2 == 0 ? -1.7f : 1.7f, 0.7f, i < 2 ? 4f : -4f), new Vector3(1.4f, 0.3f, 1.4f), lDark, new Vector3(0f, 0f, 90f));
                    Trail(lRoot, new Vector3(0f, 1f, -6f), 3f, new Color(0.6f, 0.6f, 0.6f, 0.4f), 0.5f);
                    break;
                case RivalKind.SportsCar:
                    PropFactory.BuildCarBody(lRoot, new Color(0.65f, 0.2f, 1f), true);
                    Trail(lRoot, new Vector3(-0.7f, 0.8f, -2.6f), 0.4f, new Color(3f, 0.3f, 0.3f), 0.5f);
                    Trail(lRoot, new Vector3(0.7f, 0.8f, -2.6f), 0.4f, new Color(3f, 0.3f, 0.3f), 0.5f);
                    break;
                case RivalKind.RaceCar:
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 0.6f, 0f), new Vector3(2f, 0.5f, 5.5f), new Color(0.1f, 0.6f, 1f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 0.9f, 2.6f), new Vector3(3f, 0.15f, 0.8f), lDark);
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.6f, -2.5f), new Vector3(2.8f, 0.15f, 0.9f), lDark);
                    P(PrimitiveType.Sphere, lRoot, new Vector3(0f, 1.1f, -0.2f), new Vector3(0.9f, 0.7f, 1.2f), new Color(1f, 0.9f, 0.1f));
                    for (int i = 0; i < 4; i++)
                        P(PrimitiveType.Cylinder, lRoot, new Vector3(i % 2 == 0 ? -1.3f : 1.3f, 0.55f, i < 2 ? 1.8f : -1.8f), new Vector3(1.1f, 0.35f, 1.1f), lDark, new Vector3(0f, 0f, 90f));
                    Trail(lRoot, new Vector3(0f, 0.6f, -2.8f), 1.4f, new Color(0.3f, 1.4f, 3f), 0.6f);
                    break;
                case RivalKind.Train:
                    for (int i = 0; i < 4; i++)
                    {
                        float lZ = -i * 13f;
                        P(PrimitiveType.Cube, lRoot, new Vector3(0f, 2.6f, lZ), new Vector3(3.6f, 4f, 12f), i == 0 ? Color.white : new Color(0.95f, 0.95f, 1f));
                        P(PrimitiveType.Cube, lRoot, new Vector3(0f, 2.9f, lZ), new Vector3(3.7f, 1f, 11f), new Color(0.2f, 0.35f, 0.9f));
                        G(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.2f, lZ), new Vector3(3.8f, 0.3f, 12f), new Color(0.3f, 1.5f, 3f));
                    }
                    P(PrimitiveType.Sphere, lRoot, new Vector3(0f, 2.4f, 6f), new Vector3(3.6f, 3.8f, 6f), Color.white);
                    break;
                case RivalKind.Plane:
                    P(PrimitiveType.Capsule, lRoot, Vector3.zero, new Vector3(3f, 9f, 3f), Color.white, new Vector3(90f, 0f, 0f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 0f, 0.5f), new Vector3(24f, 0.4f, 3.5f), new Color(0.85f, 0.88f, 0.95f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 2.5f, -7.5f), new Vector3(0.4f, 5f, 2.5f), UIFactory.RED);
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 0.5f, -7.5f), new Vector3(8f, 0.3f, 2f), new Color(0.85f, 0.88f, 0.95f));
                    P(PrimitiveType.Cylinder, lRoot, new Vector3(-6f, -1.2f, 1f), new Vector3(1.4f, 1.8f, 1.4f), new Color(0.6f, 0.6f, 0.7f), new Vector3(90f, 0f, 0f));
                    P(PrimitiveType.Cylinder, lRoot, new Vector3(6f, -1.2f, 1f), new Vector3(1.4f, 1.8f, 1.4f), new Color(0.6f, 0.6f, 0.7f), new Vector3(90f, 0f, 0f));
                    Trail(lRoot, new Vector3(-12f, 0f, 0f), 0.6f, new Color(1f, 1f, 1f, 0.6f), 2.5f);
                    Trail(lRoot, new Vector3(12f, 0f, 0f), 0.6f, new Color(1f, 1f, 1f, 0.6f), 2.5f);
                    break;
                case RivalKind.FighterJet:
                    P(PrimitiveType.Capsule, lRoot, Vector3.zero, new Vector3(1.8f, 6f, 1.8f), new Color(0.45f, 0.5f, 0.55f), new Vector3(90f, 0f, 0f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(-3f, 0f, -1.5f), new Vector3(6f, 0.25f, 4f), new Color(0.4f, 0.45f, 0.5f), new Vector3(0f, -35f, 0f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(3f, 0f, -1.5f), new Vector3(6f, 0.25f, 4f), new Color(0.4f, 0.45f, 0.5f), new Vector3(0f, 35f, 0f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(0f, 1.5f, -4.5f), new Vector3(0.25f, 3f, 2f), new Color(0.4f, 0.45f, 0.5f));
                    P(PrimitiveType.Sphere, lRoot, new Vector3(0f, 0.8f, 2f), new Vector3(1.1f, 0.9f, 2.4f), new Color(1f, 0.75f, 0.2f));
                    G(PrimitiveType.Sphere, lRoot, new Vector3(0f, 0f, -6.2f), new Vector3(1.4f, 1.4f, 2.5f), lFlame);
                    Trail(lRoot, new Vector3(0f, 0f, -6.5f), 1.4f, lFlame, 0.5f);
                    break;
                case RivalKind.Bullet:
                    P(PrimitiveType.Capsule, lRoot, Vector3.zero, new Vector3(0.9f, 1.6f, 0.9f), new Color(0.85f, 0.55f, 0.25f), new Vector3(90f, 0f, 0f));
                    P(PrimitiveType.Cylinder, lRoot, new Vector3(0f, 0f, -1.2f), new Vector3(0.95f, 0.5f, 0.95f), new Color(0.75f, 0.65f, 0.3f), new Vector3(90f, 0f, 0f));
                    for (int i = 0; i < 3; i++)
                        G(PrimitiveType.Cylinder, lRoot, new Vector3(0f, 0f, -3f - i * 3f), new Vector3(2.4f + i, 0.05f, 2.4f + i), new Color(1.5f, 1.5f, 1.8f, 0.3f), new Vector3(90f, 0f, 0f));
                    Trail(lRoot, new Vector3(0f, 0f, -1.5f), 0.5f, new Color(2f, 2f, 2.5f), 1.2f);
                    break;
                case RivalKind.Rockets:
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector3 lOffset = new Vector3(i * 4f, Mathf.Abs(i) * -1.5f, -Mathf.Abs(i) * 3f);
                        P(PrimitiveType.Capsule, lRoot, lOffset, new Vector3(1.6f, 3.5f, 1.6f), Color.white, new Vector3(90f, 0f, 0f));
                        P(PrimitiveType.Sphere, lRoot, lOffset + new Vector3(0f, 0f, 3f), new Vector3(1.5f, 1.5f, 1.8f), UIFactory.RED);
                        P(PrimitiveType.Cube, lRoot, lOffset + new Vector3(0f, 0f, -3f), new Vector3(3f, 0.2f, 1.2f), UIFactory.RED);
                        G(PrimitiveType.Sphere, lRoot, lOffset + new Vector3(0f, 0f, -4f), new Vector3(1.4f, 1.4f, 2.8f), lFlame);
                        Trail(lRoot, lOffset + new Vector3(0f, 0f, -4.4f), 1.6f, lFlame, 0.8f);
                    }
                    break;
                case RivalKind.Spaceship:
                    P(PrimitiveType.Cube, lRoot, Vector3.zero, new Vector3(1.6f, 1.2f, 9f), new Color(0.9f, 0.9f, 0.95f));
                    P(PrimitiveType.Sphere, lRoot, new Vector3(0f, 0.7f, 1f), new Vector3(1.2f, 0.8f, 2f), new Color(0.2f, 0.25f, 0.4f));
                    for (int i = 0; i < 4; i++)
                    {
                        float lX = i % 2 == 0 ? -1f : 1f;
                        float lY = i < 2 ? 1f : -1f;
                        GameObject lWing = P(PrimitiveType.Cube, lRoot, new Vector3(lX * 3.2f, lY * 1.2f, -2f), new Vector3(5.5f, 0.2f, 2.4f), new Color(0.85f, 0.85f, 0.9f));
                        lWing.transform.localRotation = Quaternion.Euler(0f, 0f, lX * lY * 18f);
                        G(PrimitiveType.Cylinder, lRoot, new Vector3(lX * 1.4f, lY * 0.6f, -4.6f), new Vector3(0.8f, 0.4f, 0.8f), new Color(3f, 0.6f, 0.6f), new Vector3(90f, 0f, 0f));
                        Trail(lRoot, new Vector3(lX * 1.4f, lY * 0.6f, -4.8f), 0.6f, new Color(3f, 0.4f, 0.5f), 0.4f);
                    }
                    break;
                case RivalKind.Light:
                    G(PrimitiveType.Sphere, lRoot, Vector3.zero, Vector3.one * 2.2f, new Color(6f, 6f, 6f));
                    G(PrimitiveType.Sphere, lRoot, Vector3.zero, new Vector3(1f, 1f, 6f), new Color(3f, 3.5f, 6f, 0.6f));
                    Trail(lRoot, Vector3.zero, 2.2f, new Color(4f, 4f, 6f), 1.4f);
                    Trail(lRoot, new Vector3(0f, 0.6f, 0f), 0.6f, new Color(6f, 2f, 4f), 1.8f);
                    break;
                case RivalKind.Time:
                    P(PrimitiveType.Cylinder, lRoot, Vector3.zero, new Vector3(14f, 0.8f, 14f), new Color(1f, 0.85f, 0.3f), new Vector3(90f, 0f, 0f));
                    P(PrimitiveType.Cylinder, lRoot, new Vector3(0f, 0f, 0.3f), new Vector3(12.5f, 0.5f, 12.5f), Color.white, new Vector3(90f, 0f, 0f));
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 lDir = Quaternion.Euler(0f, 0f, i * 30f) * Vector3.up;
                        P(PrimitiveType.Cube, lRoot, lDir * 5.3f + new Vector3(0f, 0f, 0.7f), new Vector3(0.5f, i % 3 == 0 ? 1.6f : 0.8f, 0.2f), lDark, new Vector3(0f, 0f, i * 30f));
                    }
                    Transform lHour = new GameObject("HourHand").transform;
                    lHour.SetParent(lRoot, false);
                    lHour.localPosition = new Vector3(0f, 0f, 0.9f);
                    P(PrimitiveType.Cube, lHour, new Vector3(0f, 1.8f, 0f), new Vector3(0.6f, 3.6f, 0.2f), lDark);
                    Transform lMinute = new GameObject("MinuteHand").transform;
                    lMinute.SetParent(lRoot, false);
                    lMinute.localPosition = new Vector3(0f, 0f, 1f);
                    P(PrimitiveType.Cube, lMinute, new Vector3(0f, 2.6f, 0f), new Vector3(0.35f, 5.2f, 0.2f), UIFactory.RED);
                    break;
                case RivalKind.Death:
                    P(PrimitiveType.Capsule, lRoot, new Vector3(0f, 0f, 0f), new Vector3(3f, 4f, 3f), new Color(0.06f, 0.04f, 0.08f));
                    P(PrimitiveType.Sphere, lRoot, new Vector3(0f, 3.5f, 0.3f), Vector3.one * 2.2f, new Color(0.06f, 0.04f, 0.08f));
                    P(PrimitiveType.Sphere, lRoot, new Vector3(0f, 3.4f, 0.8f), Vector3.one * 1.5f, new Color(0.95f, 0.92f, 0.85f));
                    G(PrimitiveType.Sphere, lRoot, new Vector3(-0.35f, 3.6f, 1.45f), Vector3.one * 0.35f, new Color(5f, 0.2f, 0.2f));
                    G(PrimitiveType.Sphere, lRoot, new Vector3(0.35f, 3.6f, 1.45f), Vector3.one * 0.35f, new Color(5f, 0.2f, 0.2f));
                    P(PrimitiveType.Cylinder, lRoot, new Vector3(2.2f, 2f, 0.5f), new Vector3(0.25f, 5f, 0.25f), new Color(0.35f, 0.25f, 0.2f), new Vector3(0f, 0f, -15f));
                    P(PrimitiveType.Cube, lRoot, new Vector3(1.2f, 6.6f, 1.2f), new Vector3(0.2f, 0.6f, 4f), new Color(0.8f, 0.85f, 0.9f), new Vector3(0f, 25f, 0f));
                    Trail(lRoot, new Vector3(0f, -2f, 0f), 3f, new Color(0.6f, 0f, 0.15f, 0.8f), 1f);
                    break;
                case RivalKind.Supernova:
                    G(PrimitiveType.Sphere, lRoot, Vector3.zero, Vector3.one * 60f, new Color(4f, 1.6f, 0.6f, 0.55f));
                    G(PrimitiveType.Sphere, lRoot, Vector3.zero, Vector3.one * 48f, new Color(6f, 4f, 2f, 0.85f));
                    G(PrimitiveType.Sphere, lRoot, Vector3.zero, Vector3.one * 72f, new Color(3f, 0.4f, 1.2f, 0.25f));
                    break;
            }
            return lRoot;
        }
    }
}
