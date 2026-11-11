using System;

namespace Com.IsartDigital.Hyperspeed
{
    public class LevelDefinition
    {
        public string title;
        public string tagline;
        // Two lines for the stage select : what the place is, and how to beat this rival.
        public string description = "";
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
                description = "A green valley to learn the run : ropes, big platforms, one loop.\nKeep close to the bike and pass it on the last straight.",
                rival = RivalKind.Moto, rivalSpeed = 150f, cruise = 120f, theme = Theme.Islands, seed = 11,
                build = b => b.Start(66f, 2400f)
                    .Hint("HOLD W / RT TO RUN, LOOK TO STEER").Meadow(4f).BigTurn(Side.Right, 80f)
                    .Hint("SHIFT / RB : DASH").Meadow(3f)
                    .Hint("JUMP ONTO THE ROPE").Ramp().Rope()
                    .Hint("CTRL + TURN : DRIFT").BigTurn(Side.Left, 110f).Hills(3f, 9f)
                    .Hint("DASH THROUGH THE GLASS").Road(2f).Platforms(4f, 1)
                    .Hint("IN THE AIR, LOOK DOWN : DIVE").Dive(220f).Meadow(3f)
                    .Hint("THE TRACK CAN TURN OVER").Loop().Ramp().Rope().BigTurn(Side.Right, 90f).Shortcut(4f).Meadow(3f).FinalRush(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE CITY BUS", tagline = "Next stop : your dust.", world = 0,
                description = "Forest hills and leaning platforms, a convoy to ride.\nThe bus is clumsy in the turns : drift past it.",
                rival = RivalKind.Bus, rivalSpeed = 162f, cruise = 130f, theme = Theme.Forest, seed = 22,
                build = b => b.Start(68f, 2600f)
                    .Meadow(3f).BigTurn(Side.Left, 90f).Platforms(5f, 2).Ramp().Rope().Loop(Side.Left).Crates(3f, 2).Dive(260f).Meadow(3f)
                    .Hairpin(Side.Right).RingArc(6).Shortcut(4f).Ramp().Rope().Hills(3f, 12f).BigTurn(Side.Right, 100f)
                    .Convoy(4f, 4).Loop().Ramp().Rope().Slabs(4f, 0.9f).FinalRush(5.5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE SPORTS CAR", tagline = "Zero to sixty in a blink. You blink faster.", world = 0,
                description = "City blocks, tunnels and an inverted run.\nIt pulls away on the straights : make it up in the air.",
                rival = RivalKind.SportsCar, rivalSpeed = 176f, cruise = 140f, theme = Theme.City, seed = 33,
                build = b => b.Start(70f, 2800f)
                    .Road(2f).BigTurn(Side.Right, 90f).Tunnel(3f).Ramp().Rope().WallTurn(Side.Right).Lasers(3f, 1.2f).Loop().Platforms(4f, 2).Hairpin(Side.Left)
                    .Slabs(4f).Inverted(2.5f).Ramp().Rope().RingArc(5).Convoy(4f, 5).Dive(260f).Meadow(3f).Shortcut(4f).Crates(3f, 3, true).Twist(3f)
                    .Ramp().Rope().BigTurn(Side.Left, 100f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE RACE CAR", tagline = "Built for one thing. So are you.", world = 0,
                description = "Night circuit : wall turns, a corkscrew, long drifts.\nEvery pad counts. So does every rope.",
                rival = RivalKind.RaceCar, rivalSpeed = 188f, cruise = 150f, theme = Theme.NeonNight, seed = 44,
                build = b => b.Start(72f, 3000f)
                    .Road(2f).WallTurn(Side.Left).BigTurn(Side.Right, 110f).Tunnel(3f).Loop(Side.Left).Ramp().Rope().Corkscrew().Platforms(4f, 2).Hairpin(Side.Right)
                    .Lasers(3f, 1f).Slabs(5f, 1.1f).Inverted(3f, Side.Right).Ramp().Rope().Meadow(3f).Shortcut(4f).Crates(3f, 4).PadRun(3f)
                    .BigTurn(Side.Left, 90f).Ramp().Rope().Loop().FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "THE BULLET TRAIN", tagline = "Runs on time. You run ahead of it.", world = 1,
                description = "Golden dunes and giant drops.\nThe train never brakes : you will have to fly.",
                rival = RivalKind.Train, rivalSpeed = 200f, cruise = 160f, theme = Theme.GoldDunes, seed = 55,
                build = b => b.Start(78f, 3200f)
                    .Meadow(3f).Dive(300f).Platforms(5f, 3).BigTurn(Side.Left, 100f).Ramp().Rope().Lasers(3f, 1f).Loop().Hairpin(Side.Left)
                    .Slabs(5f, 1.1f).Hammers(3f, 1.4f).Ramp().Rope().Dive(320f).RingArc(6).Meadow(3f).Shortcut(4f).Twist(3f).Convoy(4f, 6).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FALCONS", tagline = "Fastest thing alive. Until today.", world = 1,
                description = "A desert under an open sky : ropes high in the air.\nThe birds dive. Dive harder.",
                rival = RivalKind.Birds, rivalSpeed = 212f, cruise = 170f, theme = Theme.Desert, seed = 66,
                build = b => b.Start(80f, 3400f)
                    .Descent(3f).Dive(340f).RingArc(7).SkyRope().BigTurn(Side.Right, 100f).Platforms(5f, 3).RingArc(7).Hairpin(Side.Right).SkyRope().Dive(320f)
                    .Meadow(4f).Ramp().Rope().Corkscrew(Side.Right).Shortcut(4f).BigTurn(Side.Left, 90f).Hammers(3f, 1.2f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                description = "A volcano wakes up : lava bombs, hammers, a corkscrew.\nThe plane is huge. Stay under it.",
                rival = RivalKind.Plane, rivalSpeed = 224f, cruise = 180f, theme = Theme.Volcano, seed = 77,
                build = b => b.Start(84f, 3600f)
                    .Meadow(3f).Tunnel(3f).Hammers(3f, 1.3f).BigTurn(Side.Left, 100f).Ramp().Rope().Inverted(3f).Platforms(5f, 3).Hairpin(Side.Right).Crates(3f, 3, true)
                    .Loop().Corkscrew().Ramp().Rope().Meadow(4f).Shortcut(4f).Dive(320f).BigTurn(Side.Right, 90f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FIGHTER JET", tagline = "Mach two. Mach you.", world = 1,
                description = "A canyon of lasers, twists and meteors.\nMach two is not that fast. Prove it.",
                rival = RivalKind.FighterJet, rivalSpeed = 236f, cruise = 190f, theme = Theme.LaserCanyon, seed = 88,
                build = b => b.Start(88f, 3800f)
                    .Meadow(3f).Twist(2.6f, 2).BigTurn(Side.Right, 100f).Meteors(5f, 1.6f).Corkscrew().Ramp().Rope().Lasers(3f, 1f).Platforms(5f, 3).Loop(Side.Left)
                    .Meadow(4f).Inverted(3f, Side.Right).Convoy(4f, 6).Ramp().Rope().Shortcut(4f).BigTurn(Side.Left, 100f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "A SNIPER BULLET", tagline = "It was aimed at nothing. Now it's racing you.", world = 1,
                description = "Ice fields, frozen walls to break, straight long lines.\nThe bullet only goes straight. You don't have to.",
                rival = RivalKind.Bullet, rivalSpeed = 250f, cruise = 200f, theme = Theme.Ice, seed = 99,
                build = b => b.Start(92f, 4000f)
                    .Road(1.5f).Meadow(4f).BigTurn(Side.Right, 110f).Pillars(4f, 0.9f).Ramp().Rope().Loop().Platforms(5f, 3).Hairpin(Side.Right)
                    .Meteors(4f, 1.6f).Meadow(4f).Ramp().Rope().Shortcut(4f).Corkscrew(Side.Right).BigTurn(Side.Left, 100f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE ROCKETS", tagline = "Three of them. Pick a side.", world = 1,
                description = "Castles in the sky. Your dash now breaks blocks.\nThree rockets : stay in the middle of them.",
                rival = RivalKind.Rockets, rivalSpeed = 268f, cruise = 215f, worldBreaker = true, theme = Theme.SkyCastle, seed = 110, music = "music_race2",
                build = b => b.Start(96f, 4200f)
                    .Hint("DASHING BREAKS BLOCKS NOW").Pillars(4f, 1f).Meadow(3f).BigTurn(Side.Left, 100f).Meteors(5f, 1.4f).Dive(340f).Platforms(5f, 3).Ramp().Rope()
                    .Inverted(3f).Corkscrew().Meadow(4f).Shortcut(4f).Ramp().Rope().BigTurn(Side.Right, 100f).Loop().Ramp().Rope().FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "THE STARFIGHTER", tagline = "Lightspeed is a suggestion.", world = 2,
                description = "Open space, double loops, huge platforms adrift.\nLight speed is within reach. Hold it.",
                rival = RivalKind.Spaceship, rivalSpeed = 286f, cruise = 230f, worldBreaker = true, theme = Theme.Space, seed = 121, music = "music_race2",
                build = b => b.Start(100f, 4400f)
                    .Twist(2.4f, 2).Meadow(4f).BigTurn(Side.Right, 110f).Ramp().Rope().Platforms(6f, 3).Loop().Loop(Side.Left)
                    .RingArc(7).Meadow(4f).Shortcut(4f).Ramp().Rope().BigTurn(Side.Left, 100f).Twist(2.4f, 2, Side.Left).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "LIGHT ITSELF", tagline = "Physics called. You declined.", world = 2,
                description = "The void : almost nothing in the way, everything wide open.\nYou have to become light to keep up.",
                rival = RivalKind.Light, rivalSpeed = 306f, cruise = 245f, worldBreaker = true, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start(104f, 4600f)
                    .Road(1.5f).PadRun(4f, 0.4f).Meadow(5f).BigTurn(Side.Right, 110f).RingArc(8).Platforms(6f, 3).Ramp().Rope().Corkscrew().Meadow(5f).Dive(360f)
                    .Shortcut(4f).Ramp().Rope().BigTurn(Side.Left, 110f).Loop().Ramp().Rope().FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "TIME", tagline = "It never stops. Neither do you.", world = 2,
                description = "The ground gives way behind you.\nDon't stop. Not even once.",
                rival = RivalKind.Time, rivalSpeed = 324f, cruise = 260f, worldBreaker = true, collapse = true, theme = Theme.Void, seed = 143, music = "music_race2",
                build = b => b.Start(106f, 4800f)
                    .Hint("THE GROUND CANNOT KEEP UP").Meadow(4f).Platforms(6f, 3).BigTurn(Side.Left, 110f).Ramp().Rope().Loop().Meadow(4f).Corkscrew(Side.Right)
                    .Ramp().Rope().Twist(2.4f, 2).Shortcut(4f).BigTurn(Side.Right, 100f).Ramp().Rope().FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "THE T-REX", tagline = "Too fast. You just ran back 66 million years. Something noticed.", world = 2,
                description = "Prehistoric plains, a sky full of meteors.\nIt is behind you. Keep it there.",
                rival = RivalKind.TRex, rivalSpeed = 264f, chaser = true, chaserStart = -420f, cruise = 275f, worldBreaker = true, collapse = true, theme = Theme.Prehistoric, seed = 154, music = "music_race2",
                build = b => b.Start(110f, 5000f)
                    .Hint("DON'T LET IT CATCH YOU").Meadow(4f).BigTurn(Side.Right, 110f).Meteors(6f, 1.2f).Dive(380f).Platforms(6f, 3).Ramp().Rope().Hairpin(Side.Left)
                    .Meadow(4f).Meteors(5f, 1.1f).Loop().Shortcut(4f).Ramp().Rope().BigTurn(Side.Left, 100f).Ramp().Rope().FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "DEATH", tagline = "Don't look back. Seriously.", world = 2,
                description = "A night with no ground left behind.\nIt never tires. Neither should you.",
                rival = RivalKind.Death, rivalSpeed = 280f, chaser = true, chaserStart = -460f, cruise = 290f, worldBreaker = true, collapse = true, theme = Theme.Night, seed = 165, music = "music_race2",
                build = b => b.Start(108f, 5200f)
                    .Hint("DON'T LET DEATH CATCH YOU").Meadow(4f).BigTurn(Side.Left, 110f).Ramp().Rope().Inverted(4f).Corkscrew().Platforms(6f, 3).Hairpin(Side.Right)
                    .Meadow(5f).Ramp().Rope().Twist(2.2f, 2).Shortcut(4f).BigTurn(Side.Right, 110f).Ramp().Rope().FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "A SUPERNOVA", tagline = "Outrun the end of the world.", world = 2,
                description = "The last star collapses. The widest, fastest run of all.\nTurn to light and stay light.",
                rival = RivalKind.Supernova, rivalSpeed = 298f, chaser = true, chaserStart = -520f, cruise = 310f, worldBreaker = true, collapse = true, theme = Theme.Space, seed = 176, music = "music_race2",
                build = b => b.Start(112f, 5400f)
                    .Hint("THE STAR IS COLLAPSING. RUN.").Twist(2.2f, 2).Meadow(4f).Meteors(6f, 1f).Loop().BigTurn(Side.Right, 110f).Platforms(6f, 3).Ramp().Rope().Inverted(4f, Side.Right)
                    .Meadow(5f).Ramp().Rope().Shortcut(4f).Meteors(5f, 0.9f).BigTurn(Side.Left, 110f).Loop(Side.Left).Ramp().Rope().PadRun(4f, 0.35f).FinalRush(8f).Finish()
            }
        };

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];

        // Later stages run through their open modules for longer : they last as long as the early ones despite their speed.
        // Early stages are short and tight, the fast late ones long and huge.
        public static float Pace(int pIndex) => 1.5f + 0.9f * pIndex / (Levels.Length - 1);

        // Width on top of the recipe : the faster you go, the more room you need to steer.
        public static float WidthScale(int pIndex) => 0.95f + 0.55f * pIndex / (Levels.Length - 1);

        // Scattered pads : rare at first so the first stages stay under control.
        public static float PadDensity(int pIndex) => 0.3f + 0.7f * pIndex / (Levels.Length - 1);

        // The stage's recipe, ready to build or to bake into editable data.
        public static CourseBuilder Recipe(int pIndex, UnityEngine.Transform pRoot)
        {
            LevelDefinition lLevel = Get(pIndex);
            CourseBuilder lBuilder = new CourseBuilder(pRoot, lLevel.theme(), Profile(lLevel), lLevel.seed)
            {
                pace = Pace(pIndex), widthScale = WidthScale(pIndex), padDensity = PadDensity(pIndex)
            };
            lLevel.build(lBuilder);
            return lBuilder;
        }

        // "1-3" : world and stage within it.
        public static string Code(int pIndex)
        {
            LevelDefinition lLevel = Get(pIndex);
            int lInWorld = 0;
            for (int i = 0; i < pIndex; i++) if (Levels[i].world == lLevel.world) lInWorld++;
            return (lLevel.world + 1) + "-" + (lInWorld + 1);
        }

        // Every speed in the table is scaled by this. The run itself is calm (2026-11-11 : lower cruise), the power comes
        // from the kicks (pads, dashes, dives), which are far stronger than they were.
        public const float SPEED_SCALE = 1.35f;
        // Rivals no longer copy your speed and run well under the pace they had (2026-11-11 : "they must go twice
        // slower") : about your cruise. Using the pads and dashes beats them, the rank says by how much. Chasers keep a
        // little more bite, or they could never catch anyone.
        public const float RIVAL_SCALE = 1.8f * 0.7f;
        public const float CHASER_SCALE = 1.35f * 0.85f;

        public static SpeedProfile Profile(LevelDefinition pLevel) => SpeedProfile.For(pLevel.cruise * SPEED_SCALE, pLevel.worldBreaker);

        // The first worlds' rivals are a little gentler : the early stages teach, they do not punish.
        public static float RivalSpeed(LevelDefinition pLevel)
        {
            int lIndex = Array.IndexOf(Levels, pLevel);
            return pLevel.rivalSpeed * (pLevel.chaser ? CHASER_SCALE : RIVAL_SCALE) * (lIndex >= 0 && lIndex < 6 ? 0.9f : 1f);
        }
    }
}
