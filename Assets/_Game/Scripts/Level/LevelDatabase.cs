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
        public float chaserStart = -160f;
        public Func<Theme> theme;
        public Action<LevelBuilder> build;
        public int seed;
        public string music = "music_race";
    }

    // 3 leagues, each faster and more broken. Street teaches the moves on hard-edged platforms, Machine opens the
    // edges and stacks the world (dives, membranes, rail weaves, meteors), Cosmic makes everything collapse behind you.
    public static class LevelDatabase
    {
        public static readonly string[] WORLDS = { "STREET LEAGUE", "MACHINE LEAGUE", "COSMIC LEAGUE" };

        public static readonly LevelDefinition[] Levels =
        {
            // ---------------------------------------------------------------- STREET LEAGUE
            new LevelDefinition
            {
                title = "VS. THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                rival = RivalKind.Moto, rivalSpeed = 62f, rivalDelay = 2f, theme = Theme.Islands, seed = 11,
                build = b => b.Start().Lane(Side.Left, 34f, -3f)
                    .Hint("A / D  STEER    -    SHIFT  DASH").Run(160f).Bend(18f, 320f)
                    .Hint("BOOST PADS = SPEED").BoostStrip(120f, 2)
                    .Hint("SPACE  JUMP  (AGAIN IN THE AIR = DOUBLE JUMP)").Gap(26f).Run(60f)
                    .Hint("RAMPS KEEP YOUR MOMENTUM  -  HOLD SPACE TO GLIDE").Kicker(16f, 50f, 6f).Run(80f)
                    .Edges(EdgeKind.Lip).Hint("PICK A PLATFORM  -  HOLD A DIRECTION TO LEAVE AN EDGE").Islands(320f, 3, 24f, 5f).Run(70f)
                    .Hint("JUMP PADS SEND YOU UP").Upper(260f, 16f).Run(60f)
                    .Hint("RUN INTO THE GROOVE TO GRIND").RailSide(200f, Side.Right).Run(50f)
                    .Hint("S / CTRL IN THE AIR  -  DIVE TO THE PLATFORM BELOW").Cascade(3, 16f, 70f).Run(80f)
                    .Hint("E / CLICK  -  PUNCH").Smash(140f, 6).Run(40f)
                    .RailBridge(150f, 8f).Run(70f).Bump(70f, 6f)
                    .BoostStrip(100f, 3).Canyon(260f).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE CITY BUS", tagline = "Next stop : your dust.", world = 0,
                rival = RivalKind.Bus, rivalSpeed = 66f, theme = Theme.Islands, seed = 22,
                build = b => b.Start().Lane(Side.Right, 36f, -4f)
                    .Hint("CTRL + STEER  -  DRIFT.  RELEASE FOR A BOOST").Run(120f).Bend(-25f, 280f).Bend(25f, 280f)
                    .BoostStrip(100f, 2).Kicker(18f, 60f, 8f).Run(60f)
                    .Edges(EdgeKind.Lip).Hint("CRACKED FLOOR : CROUCH TO BREAK THROUGH").Lower(240f, 14f).Run(60f)
                    .Islands(300f, 3, 22f, 6f).Run(60f)
                    .RailBridge(140f, 6f, 15f).Run(50f).Bump(70f, 6f)
                    .Upper(240f, 16f, -8f).Run(40f)
                    .ZipLine(110f, 14f).Run(60f)
                    .Smash(140f, 8).Cascade(3, 18f, 70f).Run(60f)
                    .RailSide(180f, Side.Left).Run(40f).Canyon(280f).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE SPORTS CAR", tagline = "Zero to sixty in a blink. You blink faster.", world = 0,
                rival = RivalKind.SportsCar, rivalSpeed = 72f, theme = Theme.City, seed = 33,
                build = b => b.Start().Lane(Side.Left, 40f, -6f)
                    .Run(140f).BoostStrip(100f, 3).Bend(-30f, 300f)
                    .Edges(EdgeKind.Lip).Islands(340f, 3, 26f, 6f).Run(60f)
                    .Kicker(16f, 55f, 10f).Run(50f)
                    .Hint("WALLS ARE FOR RUNNING").WallRun(110f).Run(60f)
                    .Lower(240f, 16f).Run(40f).Bump(70f, 7f)
                    .RailWeave(240f, 2).Run(50f)
                    .Upper(260f, 18f, 10f).Run(50f)
                    .Smash(140f, 8).RailBridge(160f, 10f, -15f).Run(60f)
                    .Canyon(320f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE RACE CAR", tagline = "Built for one thing. So are you.", world = 0,
                rival = RivalKind.RaceCar, rivalSpeed = 78f, theme = Theme.City, seed = 44,
                build = b => b.Start().Lane(Side.Right, 38f, -5f)
                    .Run(120f).BoostStrip(120f, 3).Bend(35f, 260f).Bend(-35f, 260f)
                    .Edges(EdgeKind.Lip).Islands(360f, 3, 26f, 7f).Run(60f)
                    .Cascade(4, 16f, 70f).Run(60f)
                    .WallRide(200f, Side.Left, 55f).Run(40f)
                    .Kicker(18f, 65f, 10f).Run(60f)
                    .RailWeave(260f, 3).Run(50f)
                    .Upper(260f, 16f).Run(40f).Bump(70f, 6f)
                    .Smash(120f, 8).ZipLine(120f, 16f).Run(60f)
                    .Canyon(320f, 4).Finish()
            },

            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "VS. THE BULLET TRAIN", tagline = "Runs on time. You run ahead of it.", world = 1,
                rival = RivalKind.Train, rivalSpeed = 82f, theme = Theme.Desert, seed = 55,
                build = b => b.Start().Lane(Side.Left, 44f, -8f, DeckStyle.Track)
                    .Run(140f).BoostStrip(100f, 3).Edges(EdgeKind.Lip)
                    .Islands(340f, 3, 26f, 7f).Run(60f)
                    .Hint("KEEP CROUCHING TO BREAK EVERY LAYER").DiveShaft(3, 12f).Run(60f)
                    .RailBridge(170f, 10f, 20f).Run(60f)
                    .Meteors(500f, 0.9f).Upper(260f, 16f).Run(60f).Bump(70f, 6f)
                    .Kicker(18f, 70f, 12f).Run(60f)
                    .WallRun(120f).Run(60f)
                    .Lower(240f, 16f).RailWeave(240f, 3).Run(50f)
                    .Canyon(340f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                rival = RivalKind.Plane, rivalSpeed = 86f, theme = Theme.Desert, seed = 66,
                build = b => b.Start().NoLane()
                    .Run(140f).BoostStrip(120f, 3).Edges(EdgeKind.Lip)
                    .Hint("BIG DROP : DIVE AND FALL FASTER").Plunge(80f, 200f, 4).Run(60f)
                    .Islands(340f, 3, 26f, 8f).Run(60f)
                    .Meteors(400f, 0.8f).RailBridge(170f, 8f).Run(50f)
                    .Cascade(4, 18f, 72f).Run(60f)
                    .Upper(280f, 18f).Run(40f)
                    .ZipLine(130f, 18f).Run(50f).Bump(80f, 7f)
                    .RailWeave(260f, 3).Run(50f)
                    .Canyon(340f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE FIGHTER JET", tagline = "Mach two. Mach you.", world = 1,
                rival = RivalKind.FighterJet, rivalSpeed = 90f, theme = Theme.Industrial, seed = 77, music = "music_race",
                build = b => b.Start().NoLane()
                    .Run(120f).BoostStrip(120f, 3).Edges(EdgeKind.Lip)
                    .Meteors(600f, 0.7f).Islands(360f, 3, 28f, 8f).Run(60f)
                    .WallRide(220f, Side.Right, 60f).Run(40f)
                    .DiveShaft(4, 12f).Run(50f)
                    .Kicker(20f, 75f, 12f).Run(60f)
                    .RailWeave(260f, 3).Run(50f)
                    .Lower(260f, 16f).Upper(260f, 18f).Run(40f)
                    .WallRun(120f).Run(60f)
                    .Canyon(360f, 4).Finish()
            },
            new LevelDefinition
            {
                title = "VS. A SNIPER BULLET", tagline = "It was aimed at nothing. Now it's racing you.", world = 1,
                rival = RivalKind.Bullet, rivalSpeed = 95f, theme = Theme.Ice, seed = 88,
                build = b => b.Start().NoLane()
                    .Run(120f).BoostStrip(120f, 3).Edges(EdgeKind.Lip)
                    .Islands(340f, 3, 26f, 8f, true).Run(60f)
                    .Plunge(100f, 220f, 5).Run(60f)
                    .RailBridge(180f, 10f, -20f).Run(50f)
                    .Cascade(4, 20f, 75f).Run(60f)
                    .Meteors(500f, 0.7f).Upper(280f, 18f, 8f).Run(40f).Bump(80f, 7f)
                    .RailWeave(280f, 3).Run(50f)
                    .ZipLine(130f, 18f).Run(60f)
                    .Canyon(380f, 5).Finish()
            },
            new LevelDefinition
            {
                title = "VS. THE ROCKETS", tagline = "Three of them. Pick a side.", world = 1,
                rival = RivalKind.Rockets, rivalSpeed = 98f, theme = Theme.Industrial, seed = 99, music = "music_race2",
                build = b => b.Start().NoLane()
                    .Run(120f).BoostStrip(120f, 4).Edges(EdgeKind.Lip)
                    .Meteors(800f, 0.55f).Islands(360f, 3, 28f, 9f, true).Run(60f)
                    .DiveShaft(4, 14f).Run(40f)
                    .Kicker(20f, 80f, 14f).Run(60f)
                    .WallRide(220f, Side.Left, 65f).Run(40f)
                    .RailWeave(280f, 3).Run(50f)
                    .Lower(260f, 18f).Cascade(4, 20f, 75f).Run(60f)
                    .Upper(280f, 18f).Run(40f)
                    .Canyon(380f, 5).Finish()
            },

            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "VS. THE STARFIGHTER", tagline = "Lightspeed is a suggestion.", world = 2,
                rival = RivalKind.Spaceship, rivalSpeed = 102f, theme = Theme.Space, seed = 110, music = "music_race2",
                build = b => b.Start().NoLane()
                    .Run(120f).BoostStrip(120f, 4).Edges(EdgeKind.Lip)
                    .Collapse(800f, 70f).Islands(380f, 3, 28f, 9f, true).Run(60f)
                    .Plunge(120f, 240f, 5).Run(60f)
                    .RailWeave(280f, 3).Run(50f)
                    .Meteors(600f, 0.5f).Cascade(5, 18f, 72f).Run(60f)
                    .Upper(280f, 18f).Run(40f)
                    .WallRun(120f).Run(50f)
                    .RailBridge(200f, 12f, 25f).Run(60f)
                    .Canyon(400f, 5).Finish()
            },
            new LevelDefinition
            {
                title = "VS. LIGHT ITSELF", tagline = "Physics called. You declined.", world = 2,
                rival = RivalKind.Light, rivalSpeed = 108f, theme = Theme.Void, seed = 121, music = "music_race2",
                build = b => b.Start().NoLane()
                    .Run(120f).BoostStrip(140f, 4).Edges(EdgeKind.Lip)
                    .Collapse(900f, 80f).Islands(380f, 3, 30f, 10f, true).Run(60f)
                    .DiveShaft(4, 14f).Run(40f)
                    .Kicker(22f, 85f, 16f).Run(60f)
                    .RailWeave(300f, 3).Run(50f)
                    .Plunge(120f, 240f, 5).Run(60f)
                    .WallRide(220f, Side.Right, 65f).Run(40f)
                    .Lower(260f, 18f).Upper(280f, 18f).Run(40f)
                    .Canyon(420f, 5).Finish()
            },
            new LevelDefinition
            {
                title = "VS. TIME", tagline = "It never stops. Neither do you.", world = 2,
                rival = RivalKind.Time, rivalSpeed = 112f, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start().NoLane()
                    .Run(120f).BoostStrip(140f, 4).Edges(EdgeKind.Lip)
                    .Collapse(1000f, 85f).Meteors(600f, 0.5f).Islands(400f, 3, 30f, 10f, true).Run(60f)
                    .Cascade(5, 20f, 75f).Run(60f)
                    .RailWeave(300f, 3).Run(50f)
                    .Plunge(140f, 260f, 6).Run(60f)
                    .Kicker(22f, 90f, 16f).Run(50f)
                    .WallRun(130f).Run(50f)
                    .RailBridge(200f, 12f, -25f).Run(60f)
                    .Canyon(440f, 6).Finish()
            },
            new LevelDefinition
            {
                title = "VS. DEATH", tagline = "Don't look back. Seriously.", world = 2,
                rival = RivalKind.Death, rivalSpeed = 92f, rivalDelay = 2.5f, chaser = true, chaserStart = -160f, theme = Theme.Night, seed = 143, music = "music_race2",
                build = b => b.Start().NoLane()
                    .Hint("DON'T LET DEATH CATCH YOU").BoostStrip(140f, 3).Edges(EdgeKind.Lip)
                    .Collapse(900f, 88f).Islands(360f, 4, 22f, 8f).Kicker(18f, 80f, 12f).Run(40f)
                    .RailWeave(260f, 3).Run(60f)
                    .Meteors(500f, 0.45f).Cascade(5, 20f, 64f).Upper(300f, 16f).Run(40f)
                    .DiveShaft(4, 12f).Run(60f)
                    .Collapse(500f, 96f).RailBridge(220f, 10f, 20f).Islands(300f, 3, 26f, 10f).Run(40f)
                    .WallRide(200f, Side.Left, 60f).Canyon(420f, 4).BoostStrip(120f, 4)
                    .Finish()
            },
            new LevelDefinition
            {
                title = "VS. A SUPERNOVA", tagline = "Outrun the end of the world.", world = 2,
                rival = RivalKind.Supernova, rivalSpeed = 100f, rivalDelay = 2f, chaser = true, chaserStart = -200f, theme = Theme.Space, seed = 154, music = "music_race2",
                build = b => b.Start().NoLane()
                    .Hint("THE STAR IS COLLAPSING. RUN.").BoostStrip(160f, 4).Edges(EdgeKind.Lip)
                    .Collapse(1200f, 92f).Meteors(1200f, 0.4f).Islands(400f, 3, 30f, 10f, true).Run(40f)
                    .Plunge(140f, 260f, 6).Run(50f)
                    .RailWeave(300f, 3).Run(50f)
                    .Cascade(5, 20f, 72f).Run(50f)
                    .Collapse(800f, 98f).DiveShaft(4, 14f).Upper(300f, 18f).Run(40f)
                    .Kicker(22f, 90f, 16f).RailBridge(220f, 12f).Run(50f)
                    .Canyon(460f, 6).Finish()
            }
        };

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];
    }
}
