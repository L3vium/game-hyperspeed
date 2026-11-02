using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum RivalKind { Moto, Bus, SportsCar, RaceCar, Train, Plane, FighterJet, Bullet, Rockets, Spaceship, Light, Time, Death, Supernova }

    // Rival identities (names, sounds, footprints) and their placement relative to the track.
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
            float lSide = pTrackWidth * 0.5f + 12f;
            switch (pKind)
            {
                case RivalKind.Moto: return new Placement { lateral = -pTrackWidth * 0.3f, height = 0f };
                case RivalKind.Bus: return new Placement { lateral = lSide + 2f, height = 0f };
                case RivalKind.SportsCar: return new Placement { lateral = lSide, height = 0f };
                case RivalKind.RaceCar: return new Placement { lateral = lSide, height = 0f };
                case RivalKind.Train: return new Placement { lateral = lSide + 3f, height = -1f };
                case RivalKind.Plane: return new Placement { lateral = 16f, height = 26f, bob = 2f };
                case RivalKind.FighterJet: return new Placement { lateral = -12f, height = 20f, bob = 1.5f };
                case RivalKind.Bullet: return new Placement { lateral = 7f, height = 8f, bob = 0.2f };
                case RivalKind.Rockets: return new Placement { lateral = 0f, height = 18f, bob = 1.5f };
                case RivalKind.Spaceship: return new Placement { lateral = -12f, height = 16f, bob = 2f };
                case RivalKind.Light: return new Placement { lateral = 0f, height = 7f, bob = 0.5f };
                case RivalKind.Time: return new Placement { lateral = 0f, height = 32f, bob = 1f };
                case RivalKind.Death: return new Placement { lateral = 0f, height = 9f, bob = 1.5f };
                case RivalKind.Supernova: return new Placement { lateral = 0f, height = 0f };
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

        // Rough real-world footprint of each rival, used by the neutral dev placeholder.
        public static Vector3 Size(RivalKind pKind)
        {
            switch (pKind)
            {
                case RivalKind.Moto: return new Vector3(0.8f, 1.4f, 2.2f);
                case RivalKind.Bus: return new Vector3(2.6f, 3.2f, 12f);
                case RivalKind.SportsCar: return new Vector3(2f, 1.2f, 4.6f);
                case RivalKind.RaceCar: return new Vector3(2f, 1f, 5.6f);
                case RivalKind.Train: return new Vector3(3.4f, 4f, 50f);
                case RivalKind.Plane: return new Vector3(36f, 12f, 38f);
                case RivalKind.FighterJet: return new Vector3(10f, 5f, 16f);
                case RivalKind.Bullet: return new Vector3(0.6f, 0.6f, 2f);
                case RivalKind.Rockets: return new Vector3(8f, 2f, 7f);
                case RivalKind.Spaceship: return new Vector3(12f, 3f, 14f);
                case RivalKind.Light: return new Vector3(2f, 2f, 2f);
                case RivalKind.Time: return new Vector3(14f, 14f, 1f);
                case RivalKind.Death: return new Vector3(2f, 3f, 2f);
                case RivalKind.Supernova: return new Vector3(48f, 48f, 48f);
            }
            return Vector3.one * 2f;
        }

        public static Transform Build(RivalKind pKind, Transform pParent)
        {
            Transform lRoot = new GameObject("Rival_" + pKind).transform;
            lRoot.SetParent(pParent, false);
            PropFactory.Placeholder(lRoot, Size(pKind), new Color(0.35f, 0.35f, 0.4f));
            return lRoot;
        }
    }
}
