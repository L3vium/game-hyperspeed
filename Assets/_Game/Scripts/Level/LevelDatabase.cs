using System;

namespace Com.IsartDigital.Hyperspeed
{
    public class LevelDefinition
    {
        public string title;
        public string tagline;
        public int world;
        public RivalKind rival;
        public float rivalSpeed;
        public float rivalDelay = 1.5f;
        public bool chaser;
        public float chaserStart = -140f;
        public Func<Theme> theme;
        public Action<TrackBuilder> build;
        public int seed;
        public string music = "music_race";
    }

    // Clustertruck-style linear progression : 3 leagues, each faster and more absurd.
    public static class LevelDatabase
    {
        public static readonly string[] WORLDS = { "STREET LEAGUE", "MACHINE LEAGUE", "COSMIC LEAGUE" };

        public static readonly LevelDefinition[] Levels =
        {
            // ---------------------------------------------------------------- STREET LEAGUE
            new LevelDefinition
            {
                title = "VS. THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                rival = RivalKind.Moto, rivalSpeed = 33f, rivalDelay = 2f, theme = Theme.Islands, seed = 11,
                build = b => b.Start()
                    .Hint("A / D  -  STEER").Straight(60).Turn(25f, 140f).Turn(-25f, 140f)
                    .Hint("SPACE  -  JUMP").Straight(30).Gap(14f).Straight(60)
                    .Hint("SPACE IN THE AIR  -  DOUBLE JUMP").Straight(20).Gap(30f, 2f).Straight(40)
                    .Hint("BOOST PADS = SPEED").BoostStrip(80f, 2).Turn(40f, 110f)
                    .Hint("SHIFT / RMB  -  DASH").Straight(60).Hill(110f, 12f).Drop(140f, 16f)
                    .Hint("CTRL IN THE AIR  -  DIVE. LAND IT FOR SPEED!").Ramp(30f, 16f).Gap(26f, 12f).Drop(110f, 20f)
                    .Hint("E / LMB  -  PUNCH!").Crates(90f, 6).Straight(30).Wall().Straight(40)
                    .Turn(-50f, 110f).Pad().Straight(40)
                    .Hint("FLY THROUGH THE RINGS").Rings(42f, 6f, 3).Straight(50)
                    .Hint("CTRL ON THE GROUND + STEER  -  DRIFT").Turn(70f, 70f).Turn(-70f, 70f)
                    .BoostStrip(90f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE CITY BUS", tagline = "Line 42. Non-stop. Unfortunately.", world = 0,
                rival = RivalKind.Bus, rivalSpeed = 36f, theme = Theme.Islands, seed = 22,
                build = b => b.Start()
                    .Straight(40).Pad().Straight(40).Ramp(25f, 14f).Gap(22f, 8f).Straight(40)
                    .Turn(45f, 100f).Crates(100f, 8).Turn(-45f, 100f)
                    .Hill(120f, 14f).Pad().Drop(160f, 22f)
                    .Straight(20).Gap(18f).Straight(20).Gap(22f, 4f).Straight(40)
                    .Hint("GRIND RAILS : JUST LAND ON THEM").Narrow(30f, 8f).RailGap(90f, 20f).Widen(30f)
                    .Pad().Rings(46f, 8f, 3).Straight(40).Wall(0f).Straight(50)
                    .Turn(-70f, 80f).Turn(70f, 80f).BoostStrip(80f, 2)
                    .Drop(120f, 18f).Ramp(30f, 18f).Gap(34f, 14f).Straight(30).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE SPORTS CAR", tagline = "Rush hour. Cars are obstacles. Punch them.", world = 0,
                rival = RivalKind.SportsCar, rivalSpeed = 39f, theme = Theme.City, seed = 33,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Hint("TRAFFIC! DODGE OR PUNCH").Traffic(260f, 7).Turn(50f, 90f)
                    .Hint("WALLS : JUMP ON THEM TO WALL RUN").Corridor(80f).Straight(20).Ramp(20f, 10f).WallRun(70f, false).Straight(40)
                    .Turn(-60f, 80f).Traffic(200f, 6, 26f).Pad().Ramp(25f, 15f).Gap(26f, 6f)
                    .Straight(40).Hill(100f, 14f).Drop(140f, 20f).Turn(80f, 70f).Turn(-80f, 70f)
                    .Narrow(25f, 8f).RailGap(110f, -30f).Widen(25f).Traffic(160f, 5, 30f)
                    .BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE RACE CAR", tagline = "300 km/h. Pit crew is very confused.", world = 0,
                rival = RivalKind.RaceCar, rivalSpeed = 42f, theme = Theme.City, seed = 44,
                build = b => b.Start()
                    .BoostStrip(80f, 3).Turn(60f, 120f, 25f).Turn(-60f, 120f, 25f)
                    .Hint("LOOP! KEEP YOUR SPEED UP").Pad().Straight(30).Pad().Loop(26f).Straight(40)
                    .Traffic(160f, 5, 32f).Ramp(25f, 16f).Gap(30f, 10f).Straight(30)
                    .Hint("WALL RIDE : GRAVITY FOLLOWS YOU").WallRide(160f, 75f).Straight(30)
                    .Turn(-90f, 90f, 30f).BoostStrip(70f, 2).Ramp(20f, 12f).WallRun(96f).Straight(40)
                    .Pad().Straight(20).Pad().Loop(24f).Drop(140f, 18f).Turn(70f, 80f, 25f)
                    .IceField(70f).BoostStrip(60f, 2).Finish()
            },
            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "VS. THE BULLET TRAIN", tagline = "Departs on time. Arrives before you.", world = 1,
                rival = RivalKind.Train, rivalSpeed = 45f, theme = Theme.Desert, seed = 55,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Narrow(25f, 8f).RailGap(140f, 25f, 4f).Widen(20f)
                    .SideRails(120f).Turn(-50f, 100f).Ramp(25f, 15f).Gap(30f, 8f).Straight(30)
                    .Narrow(25f, 8f).RailGap(160f, -40f, 3f).RailGap(10f).Straight(10).RailGap(120f, 30f).Widen(25f)
                    .Hill(120f, 15f).Pad().Drop(160f, 24f).Crates(100f, 9).Turn(60f, 90f)
                    .WallRun(96f).Straight(30).SideRails(140f).Turn(-60f, 90f)
                    .Pad().Rings(50f, 10f, 4).BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                rival = RivalKind.Plane, rivalSpeed = 47f, theme = Theme.Desert, seed = 66,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Launch().Rings(70f, 10f, 4).Straight(40)
                    .Turn(50f, 100f).Pad().Ramp(30f, 20f).Gap(44f, 18f).Drop(120f, 22f)
                    .Hint("LAUNCH PADS : HOLD THE LINE").Straight(30).Launch().Rings(80f, 4f, 5).Straight(30)
                    .Corkscrew(180f).Straight(30).Turn(-60f, 90f, 22f).Pad().Straight(20).Launch().Gap(70f, 20f)
                    .Straight(40).Hill(100f, 15f).Drop(180f, 28f).RingLine(90f, 3).Ramp(25f, 15f).Gap(40f, 12f)
                    .BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE FIGHTER JET", tagline = "Mach 2. You brought sneakers.", world = 1,
                rival = RivalKind.FighterJet, rivalSpeed = 50f, theme = Theme.Industrial, seed = 77,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Corridor(120f, 30f).Ramp(20f, 10f).WallRun(128f).Straight(40)
                    .Hint("METEORS! PUNCH THE ROCKS OR DODGE").Meteors(260f, 0.9f).Turn(-70f, 90f, 25f)
                    .Pad().Straight(20).Pad().Loop(26f).Straight(30).WallRide(150f, 85f, 30f)
                    .Corridor(100f, -40f).IceField(80f).Ramp(25f, 15f).Gap(30f, 10f)
                    .Narrow(25f, 8f).RailGap(150f, 40f, 4f).Widen(20f).Meteors(200f, 0.7f)
                    .BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. A SNIPER BULLET", tagline = "Fired 2 km away. Do not get in front of it.", world = 1,
                rival = RivalKind.Bullet, rivalSpeed = 53f, theme = Theme.Ice, seed = 88,
                build = b => b.Start()
                    .Hint("AT HYPER SPEED YOU SMASH EVERYTHING").BoostStrip(70f, 3).IceField(110f, 5f)
                    .Turn(60f, 90f, 20f).Drop(160f, 26f).Pad().IceField(90f, 5f).Narrow(30f, 9f)
                    .Straight(60).Gap(24f, 6f).Straight(40).Widen(20f)
                    .Corkscrew(200f).Pad().Straight(20).Loop(24f).Straight(30)
                    .Ramp(20f, 10f).WallRun(96f).Straight(30).IceField(80f, 5f)
                    .Turn(-70f, 80f, 25f).Hill(100f, 14f).Drop(160f, 26f).Pad().Rings(46f, 8f, 3)
                    .IceField(70f, 4.5f).BoostStrip(60f, 2).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE ROCKETS", tagline = "Three of them. They are not friendly.", world = 1,
                rival = RivalKind.Rockets, rivalSpeed = 55f, theme = Theme.Industrial, seed = 99,
                build = b => b.Start()
                    .BoostStrip(70f, 3).Meteors(240f, 0.6f).Turn(70f, 90f, 25f)
                    .Corridor(80f).Ramp(20f, 10f).WallRun(128f).Straight(20)
                    .Pad().Straight(20).Pad().Loop(25f).Corkscrew(160f).Straight(30)
                    .Narrow(25f, 8f).RailGap(130f, -35f, 5f).Widen(20f).Meteors(220f, 0.55f)
                    .Launch().Rings(70f, 12f, 4).Drop(160f, 26f).WallRide(140f, 85f, -30f)
                    .IceField(80f, 5f).Wall(0f).Straight(30).BoostStrip(80f, 3).Finish()
            },
            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "VS. THE STARFIGHTER", tagline = "A long time ago, in a lane far, far away.", world = 2,
                rival = RivalKind.Spaceship, rivalSpeed = 58f, theme = Theme.Space, seed = 110, music = "music_race2",
                build = b => b.Start()
                    .BoostStrip(70f, 3).Corkscrew(180f).Pad().Straight(20).Loop(26f).Corkscrew(220f, 1.5f)
                    .Straight(20).Turn(80f, 90f, 30f).Ramp(20f, 10f).WallRun(160f).Straight(30)
                    .Launch().Rings(80f, 8f, 5).Straight(30).WallRide(160f, 90f, 40f).WallRide(160f, -90f, -40f)
                    .Meteors(200f, 0.6f).Pad().Loop(24f).Narrow(25f, 8f).RailGap(160f, 50f, 5f).Widen(20f)
                    .IceField(70f, 5f).BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. LIGHT ITSELF", tagline = "299 792 458 m/s. Good luck.", world = 2,
                rival = RivalKind.Light, rivalSpeed = 61f, theme = Theme.Void, seed = 121, music = "music_race2",
                build = b => b.Start()
                    .BoostStrip(100f, 4).Drop(200f, 28f).IceField(90f, 4.5f).Pad().Straight(20).Loop(28f)
                    .BoostStrip(80f, 3).Corkscrew(200f, 2f).Pad().Launch().Rings(90f, 10f, 5)
                    .Drop(220f, 30f).Ramp(20f, 10f).WallRun(160f).Straight(30).BoostStrip(60f, 3)
                    .WallRide(180f, 90f, 60f).IceField(80f, 4.5f).Pad().Loop(26f).Drop(200f, 30f)
                    .BoostStrip(100f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. TIME", tagline = "Tick. Tock. You are late.", world = 2,
                rival = RivalKind.Time, rivalSpeed = 63f, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start()
                    .BoostStrip(80f, 3).Turn(90f, 80f, 30f).Turn(-90f, 80f, 30f).Pad().Loop(25f)
                    .Corkscrew(200f).Ramp(20f, 10f).WallRun(160f).Straight(20).Meteors(200f, 0.5f)
                    .Narrow(25f, 8f).RailGap(150f, -60f, 6f).Widen(20f).IceField(80f, 4.5f)
                    .Launch().Rings(80f, 10f, 5).WallRide(160f, 90f, 45f).Drop(220f, 30f)
                    .Pad().Loop(24f).Corkscrew(160f, 1f).BoostStrip(100f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. DEATH", tagline = "It is right behind you. Literally.", world = 2,
                rival = RivalKind.Death, rivalSpeed = 50f, chaser = true, chaserStart = -130f, rivalDelay = 2.5f, theme = Theme.Night, seed = 143, music = "music_race2",
                build = b => b.Start()
                    .Hint("DON'T LET DEATH CATCH YOU").BoostStrip(70f, 2).Turn(60f, 90f, 25f).Crates(80f, 7)
                    .Ramp(20f, 10f).WallRun(128f).Straight(30).Meteors(200f, 0.6f).Pad().Loop(25f)
                    .Narrow(25f, 8f).RailGap(140f, 40f, 5f).Widen(20f).Corkscrew(180f)
                    .Drop(200f, 28f).IceField(80f, 5f).Turn(-80f, 80f, 30f).Launch().Rings(80f, 12f, 4)
                    .WallRide(160f, 85f, -40f).BoostStrip(90f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. A SUPERNOVA", tagline = "A star just exploded. Outrun the light cone.", world = 2,
                rival = RivalKind.Supernova, rivalSpeed = 56f, chaser = true, chaserStart = -170f, rivalDelay = 2f, theme = Theme.Space, seed = 154, music = "music_race2",
                build = b => b.Start()
                    .Hint("OUTRUN THE EXPLOSION!").BoostStrip(100f, 4).Drop(200f, 26f).Pad().Loop(27f)
                    .Corkscrew(220f, 2f).Ramp(20f, 10f).WallRun(160f).Straight(20).Meteors(240f, 0.4f)
                    .Launch().Rings(90f, 12f, 5).IceField(90f, 4.5f).WallRide(180f, 90f, 60f)
                    .Narrow(25f, 8f).RailGap(160f, -50f, 6f).Widen(20f).Pad().Loop(25f)
                    .Drop(240f, 30f).BoostStrip(120f, 5).Finish()
            },
        };

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];
    }
}
