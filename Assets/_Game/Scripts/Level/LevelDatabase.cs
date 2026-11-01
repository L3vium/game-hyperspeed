using System;
using L = Com.IsartDigital.Hyperspeed.TrackBuilder.Lane;

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
    // Every stage mixes split lanes (high / low / wall / ceiling), an upside-down stretch, drift bends,
    // momentum ramps, a zip line and ring chains.
    public static class LevelDatabase
    {
        public static readonly string[] WORLDS = { "STREET LEAGUE", "MACHINE LEAGUE", "COSMIC LEAGUE" };

        public static readonly LevelDefinition[] Levels =
        {
            // ---------------------------------------------------------------- STREET LEAGUE
            new LevelDefinition
            {
                title = "VS. THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                rival = RivalKind.Moto, rivalSpeed = 34f, rivalDelay = 2f, theme = Theme.Islands, seed = 11,
                build = b => b.Start()
                    .Hint("A / D  -  STEER      SPACE ON GO  -  PERFECT START").Straight(50).Turn(30f, 130f).Turn(-30f, 130f)
                    .Hint("SPACE  -  JUMP  (TWICE IN THE AIR = DOUBLE JUMP)").Straight(20).Gap(16f).Straight(40)
                    .Hint("BOOST PADS AND RINGS = SPEED").BoostStrip(70f, 2).Ramp(30f, 18f).Gap(34f, 6f).Straight(30)
                    .Hint("CTRL + STEER  -  DRIFT.  RELEASE FOR A BOOST").Chicane(35f, 70f)
                    .Hint("TAKE ANOTHER ROAD : STEER OFF THE FORK").Split(160f, L.High, L.Low)
                    .Hint("SHIFT / RMB  -  DASH").Hill(110f, 12f).Drop(140f, 18f)
                    .Hint("CTRL IN THE AIR  -  DIVE.  LAND IT JUST RIGHT").Ramp(30f, 18f).Gap(30f, 14f).Drop(100f, 20f)
                    .Hint("E / LMB  -  PUNCH!").Crates(90f, 6).Straight(20).Wall().Straight(30)
                    .Hint("JUMP TO GRAB THE ZIP LINE").Straight(20).ZipLine(90f, 14f).Straight(40)
                    .Pad().Rings(46f, 6f, 3).Straight(30).Hairpin(140f, 55f).BoostStrip(90f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE CITY BUS", tagline = "Line 42. Non-stop. Unfortunately.", world = 0,
                rival = RivalKind.Bus, rivalSpeed = 37f, theme = Theme.Islands, seed = 22,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Ramp(30f, 18f).Gap(30f, 8f).Straight(30).Chicane(40f, 60f)
                    .Split(180f, L.Wall, L.High).Crates(80f, 8)
                    .Hint("GRIND RAILS : JUST LAND ON THEM").Narrow(30f, 8f).RailGap(90f, 20f).Widen(30f)
                    .Hairpin(-150f, 50f).Pad().Ramp(30f, 20f).Gap(36f, 4f).Straight(20)
                    .Hint("THE ROAD FLIPS : GRAVITY FOLLOWS YOU").Pad().FlipOver(220f)
                    .ZipLine(100f, 16f).Straight(30).Split(160f, L.Low, L.Ceiling)
                    .Pad().Rings(50f, 8f, 4).Chicane(-40f, 60f).BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE SPORTS CAR", tagline = "Rush hour. Cars are obstacles. Punch them.", world = 0,
                rival = RivalKind.SportsCar, rivalSpeed = 40f, theme = Theme.City, seed = 33,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Hint("TRAFFIC! DODGE OR PUNCH").Traffic(200f, 6).Chicane(45f, 60f)
                    .Hint("WALLS : JUMP ON THEM TO WALL RUN").Corridor(60f).Ramp(20f, 10f).WallRun(80f).Straight(30)
                    .Split(200f, L.High, L.Wall).Traffic(160f, 5, 26f).Pad().Ramp(30f, 20f).Gap(34f, 6f)
                    .Straight(20).Hairpin(160f, 46f).Pad().FlipOver(200f)
                    .Narrow(25f, 8f).RailGap(110f, -30f).Widen(25f).ZipLine(90f, 12f).Straight(20)
                    .Split(160f, L.Ceiling, L.Low).Chicane(-35f, 65f).BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE RACE CAR", tagline = "300 km/h. Pit crew is very confused.", world = 0,
                rival = RivalKind.RaceCar, rivalSpeed = 43f, theme = Theme.City, seed = 44,
                build = b => b.Start()
                    .BoostStrip(80f, 3).Chicane(40f, 70f)
                    .Hint("LOOP! KEEP YOUR SPEED UP").Pad().Straight(30).Pad().Loop(26f).Straight(30)
                    .Split(200f, L.Ceiling, L.High).Traffic(140f, 5, 32f).Ramp(30f, 20f).Gap(36f, 10f).Straight(20)
                    .WallRide(160f, 75f).Hairpin(-150f, 50f).BoostStrip(60f, 2).Ramp(20f, 12f).WallRun(96f).Straight(30)
                    .Pad().FlipOver(220f).ZipLine(100f, 16f).Straight(20).Pad().Loop(24f)
                    .Split(160f, L.Wall, L.Low).IceField(70f).BoostStrip(60f, 2).Finish()
            },
            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "VS. THE BULLET TRAIN", tagline = "Departs on time. Arrives before you.", world = 1,
                rival = RivalKind.Train, rivalSpeed = 45f, theme = Theme.Desert, seed = 55,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Narrow(25f, 8f).RailGap(140f, 25f, 4f).Widen(20f)
                    .Split(200f, L.High, L.Ceiling).Chicane(45f, 60f).Ramp(30f, 18f).Gap(34f, 8f).Straight(20)
                    .ZipLine(120f, 20f).Straight(20).SideRails(140f).Hairpin(150f, 50f)
                    .Pad().FlipOver(240f).Crates(90f, 9).Split(180f, L.Wall, L.Wall)
                    .Ramp(20f, 10f).WallRun(96f).Straight(30).Pad().Rings(56f, 10f, 4).BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                rival = RivalKind.Plane, rivalSpeed = 47f, theme = Theme.Desert, seed = 66,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Launch().Rings(70f, 10f, 4).Straight(30).Chicane(40f, 65f)
                    .Hint("LAUNCH PADS : HOLD THE LINE").Pad().Ramp(30f, 22f).Gap(46f, 18f).Drop(100f, 22f)
                    .Split(200f, L.Ceiling, L.Wall).ZipLine(130f, 24f).Straight(20)
                    .Corkscrew(180f).Hairpin(-150f, 52f).Pad().Straight(20).Launch().Rings(80f, 12f, 5)
                    .Pad().FlipOver(220f).Split(160f, L.High, L.Low).Ramp(30f, 18f).Gap(40f, 12f)
                    .BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE FIGHTER JET", tagline = "Mach 2. You brought sneakers.", world = 1,
                rival = RivalKind.FighterJet, rivalSpeed = 48f, theme = Theme.Industrial, seed = 77,
                build = b => b.Start()
                    .BoostStrip(60f, 2).Corridor(100f, 30f).Ramp(20f, 10f).WallRun(110f).Straight(30)
                    .Hint("METEORS! PUNCH THE ROCKS OR DODGE").Meteors(200f, 0.9f).Chicane(-45f, 60f)
                    .Pad().Straight(20).Pad().Loop(26f).Split(200f, L.Ceiling, L.High)
                    .Pad().FlipOver(220f).IceField(70f).Ramp(30f, 20f).Gap(36f, 10f).Straight(20)
                    .ZipLine(110f, 18f).Straight(20).Narrow(25f, 8f).RailGap(130f, 40f, 4f).Widen(20f)
                    .Hairpin(150f, 50f).Meteors(160f, 0.7f).BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. A SNIPER BULLET", tagline = "Fired 2 km away. Do not get in front of it.", world = 1,
                rival = RivalKind.Bullet, rivalSpeed = 50f, theme = Theme.Ice, seed = 88,
                build = b => b.Start()
                    .Hint("AT HYPER SPEED YOU SMASH EVERYTHING").BoostStrip(70f, 3).IceField(100f, 5f)
                    .Chicane(45f, 60f).Drop(140f, 26f).Split(200f, L.Low, L.Ceiling)
                    .Corkscrew(200f).Pad().Straight(20).Loop(24f).Straight(20)
                    .ZipLine(120f, 20f).Straight(20).Ramp(20f, 10f).WallRun(96f).Straight(30).IceField(80f, 5f)
                    .Hairpin(-155f, 48f).Pad().FlipOver(200f).Split(160f, L.Wall, L.High)
                    .Pad().Rings(50f, 8f, 4).IceField(70f, 4.5f).BoostStrip(60f, 2).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE ROCKETS", tagline = "Three of them. They are not friendly.", world = 1,
                rival = RivalKind.Rockets, rivalSpeed = 52f, theme = Theme.Industrial, seed = 99,
                build = b => b.Start()
                    .BoostStrip(70f, 3).Meteors(200f, 0.6f).Chicane(45f, 60f)
                    .Split(200f, L.Ceiling, L.Wall).Pad().Straight(20).Pad().Loop(25f).Corkscrew(160f)
                    .ZipLine(130f, 22f).Straight(20).Narrow(25f, 8f).RailGap(130f, -35f, 5f).Widen(20f)
                    .Launch().Rings(70f, 12f, 4).Drop(140f, 26f).Pad().FlipOver(220f)
                    .Hairpin(150f, 50f).Split(180f, L.High, L.Low).IceField(80f, 5f).Wall(0f).Straight(30)
                    .BoostStrip(80f, 3).Finish()
            },
            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "VS. THE STARFIGHTER", tagline = "A long time ago, in a lane far, far away.", world = 2,
                rival = RivalKind.Spaceship, rivalSpeed = 53f, theme = Theme.Space, seed = 110, music = "music_race2",
                build = b => b.Start()
                    .BoostStrip(70f, 3).Corkscrew(180f).Pad().Straight(20).Loop(26f).Corkscrew(220f, 1f)
                    .Split(220f, L.Ceiling, L.Ceiling).Chicane(45f, 60f).Ramp(20f, 10f).WallRun(110f).Straight(30)
                    .Launch().Rings(80f, 8f, 5).Straight(20).ZipLine(130f, 24f).Straight(20)
                    .WallRide(160f, 80f, 40f).Straight(30).WallRide(160f, -80f, -40f)
                    .Pad().FlipOver(240f).Hairpin(-150f, 50f).Split(160f, L.High, L.Wall)
                    .IceField(70f, 5f).BoostStrip(80f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. LIGHT ITSELF", tagline = "299 792 458 m/s. Good luck.", world = 2,
                rival = RivalKind.Light, rivalSpeed = 55f, theme = Theme.Void, seed = 121, music = "music_race2",
                build = b => b.Start()
                    .BoostStrip(100f, 4).Drop(200f, 28f).IceField(90f, 4.5f).Pad().Straight(20).Loop(28f)
                    .Split(220f, L.High, L.Ceiling).Corkscrew(200f, 2f).Pad().Launch().Rings(90f, 10f, 5)
                    .Chicane(50f, 55f).ZipLine(140f, 26f).Straight(20).Pad().FlipOver(240f)
                    .Ramp(20f, 10f).WallRun(120f).Straight(30).BoostStrip(60f, 3)
                    .Hairpin(155f, 50f).Split(180f, L.Wall, L.Low).Drop(180f, 30f).BoostStrip(100f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. TIME", tagline = "Tick. Tock. You are late.", world = 2,
                rival = RivalKind.Time, rivalSpeed = 57f, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start()
                    .BoostStrip(80f, 3).Chicane(50f, 55f).Pad().Loop(25f)
                    .Split(220f, L.Ceiling, L.Wall).Ramp(20f, 10f).WallRun(110f).Straight(20).Meteors(180f, 0.5f)
                    .ZipLine(140f, 24f).Straight(20).Narrow(25f, 8f).RailGap(150f, -60f, 6f).Widen(20f)
                    .IceField(80f, 4.5f).Launch().Rings(80f, 10f, 5).Pad().FlipOver(240f)
                    .Hairpin(-155f, 48f).Split(180f, L.High, L.Ceiling).Pad().Loop(24f).Corkscrew(160f, 1f)
                    .BoostStrip(100f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. DEATH", tagline = "It is right behind you. Literally.", world = 2,
                rival = RivalKind.Death, rivalSpeed = 46f, chaser = true, chaserStart = -130f, rivalDelay = 2.5f, theme = Theme.Night, seed = 143, music = "music_race2",
                build = b => b.Start()
                    .Hint("DON'T LET DEATH CATCH YOU").BoostStrip(70f, 2).Chicane(45f, 60f).Crates(80f, 7)
                    .Split(200f, L.Low, L.High).Ramp(20f, 10f).WallRun(110f).Straight(30).Meteors(180f, 0.6f)
                    .Pad().Loop(25f).ZipLine(130f, 22f).Straight(20).Narrow(25f, 8f).RailGap(140f, 40f, 5f).Widen(20f)
                    .Pad().FlipOver(220f).Hairpin(150f, 50f).Split(180f, L.Ceiling, L.Wall)
                    .Launch().Rings(80f, 12f, 4).BoostStrip(90f, 3).Finish()
            },
            new LevelDefinition
            {
                title = "VS. A SUPERNOVA", tagline = "A star just exploded. Outrun the light cone.", world = 2,
                rival = RivalKind.Supernova, rivalSpeed = 50f, chaser = true, chaserStart = -170f, rivalDelay = 2f, theme = Theme.Space, seed = 154, music = "music_race2",
                build = b => b.Start()
                    .Hint("OUTRUN THE EXPLOSION!").BoostStrip(100f, 4).Drop(180f, 26f).Pad().Loop(27f)
                    .Split(220f, L.Ceiling, L.High).Corkscrew(220f, 2f).Ramp(20f, 10f).WallRun(110f).Straight(20)
                    .Meteors(200f, 0.4f).ZipLine(140f, 26f).Straight(20).Launch().Rings(90f, 12f, 5)
                    .Chicane(50f, 55f).Pad().FlipOver(240f).Split(180f, L.Wall, L.Low)
                    .Hairpin(-150f, 50f).IceField(90f, 4.5f).Drop(200f, 30f).BoostStrip(120f, 5).Finish()
            },
        };

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];
    }
}
