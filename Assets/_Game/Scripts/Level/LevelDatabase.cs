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
        public float rivalDelay;
        public bool chaser;
        public float chaserStart = -300f;
        public float cruise = 120f;
        public bool worldBreaker;
        public bool collapse;
        public Func<Theme> theme;
        public Action<CourseBuilder> build;
        public int seed;
        public string music = "music_race";
    }

    // 16 stages, each faster than the one before and each with its own idea : on foot against vehicles, then against what
    // no vehicle can match. You end up so fast you tear the ground open, turn to light and overtake time itself.
    public static class LevelDatabase
    {
        public static readonly string[] WORLDS = { "STREET LEAGUE", "MACHINE LEAGUE", "COSMIC LEAGUE" };

        public static readonly LevelDefinition[] Levels =
        {
            // ---------------------------------------------------------------- STREET LEAGUE
            new LevelDefinition
            {
                title = "THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                rival = RivalKind.Moto, rivalSpeed = 150f, cruise = 120f, theme = Theme.Islands, seed = 11,
                build = b => b.Start(76f, 2400f)
                    .Hint("MOUSE : YOU GO WHERE YOU LOOK").Hills(4f, 9f).Sweeps(3)
                    .Hint("SHIFT : DASH    HOLD : PUSH").PadRun(2.5f).Slabs(5f, 0.8f)
                    .Hint("IN THE AIR, LOOK DOWN : DIVE").Dive(220f).Sweeps(3)
                    .Hint("CTRL + A / D : DRIFT").Hairpin(Side.Left).Road(2f)
                    .Hint("THE TRACK CAN TURN OVER").Loop().RingArc(5)
                    .Hint("DASH THROUGH CRATES").Crates(3f, 2).Sweeps(3).Corridor(3f).Hills(3f, 12f).Twist(3f)
                    .Hint("JUMP ONTO THE ROPE").Rope().Sweeps(2).Plateaus(2).Turn(Side.Right, 70f).PadRun(2f).Slabs(4f).FinalRush(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE CITY BUS", tagline = "Next stop : your dust.", world = 0,
                rival = RivalKind.Bus, rivalSpeed = 162f, cruise = 130f, theme = Theme.Forest, seed = 22,
                build = b => b.Start(76f, 2600f)
                    .Hills(3f, 10f).Sweeps(4).Slabs(6f, 0.9f).Loop(Side.Left).Crates(3f, 3).Dive(260f).Sweeps(3).Pillars(4f, 0.8f)
                    .Hairpin(Side.Right).RingArc(6).Twist(3f, 1, Side.Left).Plateaus(3).Corridor(3f).Sweeps(4).Rope().Hills(3f, 12f)
                    .Convoy(4f, 5).Loop().PadRun(3f).Slabs(5f).FinalRush(5.5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE SPORTS CAR", tagline = "Zero to sixty in a blink. You blink faster.", world = 0,
                rival = RivalKind.SportsCar, rivalSpeed = 176f, cruise = 140f, theme = Theme.City, seed = 33,
                build = b => b.Start(72f, 2800f)
                    .Road(2f).Sweeps(4).Tunnel(3f).WallTurn(Side.Right).Lasers(4f, 1.1f).Loop().Sweeps(3).Hairpin(Side.Left)
                    .Slabs(5f).Inverted(2.5f).RingArc(5).Convoy(4f, 6).Dive(260f).Sweeps(4).Corridor(3f).Crates(3f, 3, true).Twist(3f)
                    .PadRun(3f).Hills(3f, 12f).Lasers(3f, 1f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE RACE CAR", tagline = "Built for one thing. So are you.", world = 0,
                rival = RivalKind.RaceCar, rivalSpeed = 188f, cruise = 150f, theme = Theme.NeonNight, seed = 44,
                build = b => b.Start(72f, 3000f)
                    .Road(2f).WallTurn(Side.Left).Sweeps(5).Tunnel(3f).Loop(Side.Left).Corkscrew().Sweeps(3).Pillars(4f, 1f).Hairpin(Side.Right)
                    .WallTurn(Side.Right).Lasers(3f, 1f).Slabs(5f, 1.1f).Inverted(3f, Side.Right).Rope().Sweeps(4).Crates(3f, 4).PadRun(3f)
                    .Hills(3f, 14f).Loop().FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "THE BULLET TRAIN", tagline = "Runs on time. You run ahead of it.", world = 1,
                rival = RivalKind.Train, rivalSpeed = 200f, cruise = 160f, theme = Theme.GoldDunes, seed = 55,
                build = b => b.Start(80f, 3200f)
                    .Hills(3f, 12f).Dive(300f).Plateaus(3).Sweeps(4).Lasers(4f, 0.9f).Rope().Corridor(3f).Loop().Hairpin(Side.Left)
                    .Slabs(6f, 1.1f).Hammers(4f).Dive(320f).RingArc(6).Sweeps(4).Twist(3f).Convoy(4f, 7).PadRun(3f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FALCONS", tagline = "Fastest thing alive. Until today.", world = 1,
                rival = RivalKind.Birds, rivalSpeed = 212f, cruise = 170f, theme = Theme.Desert, seed = 66,
                build = b => b.Start(80f, 3400f)
                    .Descent(3f).Dive(340f).RingArc(7).SkyRope().Sweeps(3).Plateaus(3).RingArc(7).Hairpin(Side.Right).SkyRope().Dive(320f)
                    .Hills(3f, 14f).Slabs(5f, 1.2f).RingArc(6).Corkscrew(Side.Right).Sweeps(4).Hammers(3f, 1f).PadRun(3f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                rival = RivalKind.Plane, rivalSpeed = 224f, cruise = 180f, theme = Theme.Industrial, seed = 77,
                build = b => b.Start(74f, 3600f)
                    .Road(2f).Tunnel(3f).Hammers(4f, 1f).Sweeps(4).Inverted(3f).Lasers(4f, 0.85f).Hairpin(Side.Right).Crates(3f, 4, true).WallTurn(Side.Left)
                    .Loop().Corkscrew().Slabs(5f, 1.2f).Tunnel(2.5f).Sweeps(4).Dive(320f).Hammers(3f, 0.9f).PadRun(3f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FIGHTER JET", tagline = "Mach two. Mach you.", world = 1,
                rival = RivalKind.FighterJet, rivalSpeed = 236f, cruise = 190f, theme = Theme.LaserCanyon, seed = 88,
                build = b => b.Start(74f, 3800f)
                    .Hills(3f, 14f).Twist(2.6f, 2).Sweeps(5).Meteors(5f, 1.4f).Corkscrew().WallTurn(Side.Right).Lasers(3f, 0.8f).Plateaus(3).Loop(Side.Left)
                    .Sweeps(4).Inverted(3f, Side.Right).Convoy(4f, 8).Rope().Slabs(5f, 1.3f).PadRun(3f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "A SNIPER BULLET", tagline = "It was aimed at nothing. Now it's racing you.", world = 1,
                rival = RivalKind.Bullet, rivalSpeed = 250f, cruise = 200f, theme = Theme.Ice, seed = 99,
                build = b => b.Start(66f, 4000f)
                    .Road(1.5f).Tunnel(3f).Sweeps(5).Pillars(5f, 1.2f).Corridor(4f).Loop().Pillars(4f, 1.3f).Hairpin(Side.Right).Slabs(5f, 1.3f)
                    .Meteors(4f, 1.3f).WallTurn(Side.Left).Sweeps(5).Twist(2.6f).Corkscrew(Side.Right).PadRun(3f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE ROCKETS", tagline = "Three of them. Pick a side.", world = 1,
                rival = RivalKind.Rockets, rivalSpeed = 268f, cruise = 215f, worldBreaker = true, theme = Theme.SkyCastle, seed = 110, music = "music_race2",
                build = b => b.Start(72f, 4200f)
                    .Hint("DASHING BREAKS BLOCKS NOW").Pillars(5f, 1.3f).Crates(3f, 5, true).Sweeps(4).Meteors(5f, 1.1f).Dive(340f).Plateaus(3).Hammers(4f, 0.8f)
                    .Inverted(3f).Corkscrew().Lasers(4f, 0.75f).Sweeps(5).Pillars(4f, 1.4f).Loop().PadRun(3f).FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "THE STARFIGHTER", tagline = "Lightspeed is a suggestion.", world = 2,
                rival = RivalKind.Spaceship, rivalSpeed = 286f, cruise = 230f, worldBreaker = true, theme = Theme.Space, seed = 121, music = "music_race2",
                build = b => b.Start(70f, 4400f)
                    .Twist(2.4f, 2).Tunnel(3f).Inverted(3f, Side.Right).Sweeps(4).Lasers(4f, 0.7f).Corkscrew(Side.Right).Slabs(5f, 1.4f).Loop().Loop(Side.Left)
                    .RingArc(7).Tunnel(3f).WallTurn(Side.Right).Sweeps(5).Twist(2.4f, 2, Side.Left).PadRun(3f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "LIGHT ITSELF", tagline = "Physics called. You declined.", world = 2,
                rival = RivalKind.Light, rivalSpeed = 306f, cruise = 245f, worldBreaker = true, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start(66f, 4600f)
                    .Road(1.5f).PadRun(4f, 0.35f).Slabs(6f, 1.5f).Sweeps(5).RingArc(8).Plateaus(3).Corkscrew().Sweeps(4).PadRun(4f, 0.35f).Dive(360f)
                    .Inverted(3f).RingArc(8).Corridor(4f).Loop().Sweeps(5).FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "TIME", tagline = "It never stops. Neither do you.", world = 2,
                rival = RivalKind.Time, rivalSpeed = 324f, cruise = 260f, worldBreaker = true, collapse = true, theme = Theme.Void, seed = 143, music = "music_race2",
                build = b => b.Start(66f, 4800f)
                    .Hint("THE GROUND CANNOT KEEP UP").Hills(3f, 14f).Slabs(6f, 1.5f).Hammers(4f, 0.7f).Sweeps(5).Loop().Plateaus(3).Corkscrew(Side.Right).Hammers(3f, 0.65f)
                    .Twist(2.4f, 2).Sweeps(5).Slabs(5f, 1.6f).PadRun(3f).FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "THE T-REX", tagline = "Too fast. You just ran back 66 million years. Something noticed.", world = 2,
                rival = RivalKind.TRex, rivalSpeed = 264f, chaser = true, chaserStart = -420f, cruise = 275f, worldBreaker = true, collapse = true, theme = Theme.Prehistoric, seed = 154, music = "music_race2",
                build = b => b.Start(84f, 5000f)
                    .Hint("DON'T LET IT CATCH YOU").Hills(3f, 15f).Pillars(5f, 1.3f).Sweeps(5).Meteors(6f, 0.9f).Dive(380f).Plateaus(3).Hairpin(Side.Left).Slabs(5f, 1.6f)
                    .Meteors(5f, 0.8f).Loop().Sweeps(5).Crates(3f, 5, true).PadRun(3f).FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "DEATH", tagline = "Don't look back. Seriously.", world = 2,
                rival = RivalKind.Death, rivalSpeed = 280f, chaser = true, chaserStart = -460f, cruise = 290f, worldBreaker = true, collapse = true, theme = Theme.Night, seed = 165, music = "music_race2",
                build = b => b.Start(62f, 5200f)
                    .Hint("DON'T LET DEATH CATCH YOU").Tunnel(4f).Lasers(5f, 0.65f).Sweeps(5).Inverted(4f).Corkscrew().Hammers(4f, 0.6f).Hairpin(Side.Right).Corridor(4f)
                    .Slabs(6f, 1.7f).Twist(2.2f, 2).Lasers(4f, 0.6f).Sweeps(5).FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "A SUPERNOVA", tagline = "Outrun the end of the world.", world = 2,
                rival = RivalKind.Supernova, rivalSpeed = 298f, chaser = true, chaserStart = -520f, cruise = 310f, worldBreaker = true, collapse = true, theme = Theme.Space, seed = 176, music = "music_race2",
                build = b => b.Start(70f, 5400f)
                    .Hint("THE STAR IS COLLAPSING. RUN.").Twist(2.2f, 2).Meteors(6f, 0.75f).Loop().Corkscrew(Side.Right).Sweeps(5).Slabs(6f, 1.8f).Inverted(4f, Side.Right).Lasers(4f, 0.55f)
                    .Plateaus(3).Hammers(4f, 0.55f).Tunnel(4f).Meteors(5f, 0.7f).Sweeps(6).Loop(Side.Left).PadRun(4f, 0.3f).FinalRush(8f).Finish()
            }
        };

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];

        // Later stages run through their open modules for longer : they last as long as the early ones despite their speed.
        public static float Pace(int pIndex) => 1.5f + 0.4f * pIndex / (Levels.Length - 1);

        public static SpeedProfile Profile(LevelDefinition pLevel) => SpeedProfile.For(pLevel.cruise, pLevel.worldBreaker);
    }
}
