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
        // The rival's own pace, as a share of the stage's cruise (chasers : how fast they hunt you from behind).
        public float rivalPace = 1.2f;
        public float rivalDelay;
        public bool chaser;
        public float chaserStart = -300f;
        // Run speed with no boost at all, in m/s : set from the stage's place in the game (LevelDatabase.CruiseOf).
        public float cruise = 110f;
        // Kept for older data : hyper and light exist on every stage now.
        public bool hyper;
        public float lightSpeed;
        public int lightPads = 24;
        public bool worldBreaker;
        public bool collapse;
        public Func<Theme> theme;
        public Action<CourseBuilder> build;
        public int seed;
        public string music = "music_race";
    }

    // 17 stages, each faster AND bigger than the one before (2026-11-14 : 1-1 runs at 110 m/s in a small valley, 3-6 runs
    // ten times faster in a world ten times bigger). Every stage is one big terrain with many routes at once : pads on the
    // ground, a rope above them, rings in the air left and right, sky lanes, platforms stepping down with rocks floating
    // between them, loops, spirals, hammers, lasers, and a last straight full of pads. Early stages are easy, late ones hard.
    public static class LevelDatabase
    {
        public static readonly string[] WORLDS = { "STREET LEAGUE", "MACHINE LEAGUE", "COSMIC LEAGUE" };

        public static readonly LevelDefinition[] Levels =
        {
            // ---------------------------------------------------------------- STREET LEAGUE
            new LevelDefinition
            {
                title = "THE DOGS", tagline = "Four legs each. You have two. Run anyway.", world = 0,
                description = "A green park valley to learn the run : pads, a rope, rings, a spiral.\nThe pack is quick but it tires : pass it on the last straight.",
                rival = RivalKind.Dogs, rivalPace = 1.02f, theme = Theme.Islands, seed = 7,
                build = b => b.Start(64f, 2200f)
                    .Hint("MOUSE / RIGHT STICK : STEER").Road(2f).Meadow(3f)
                    .Hint("SHIFT / RB : DASH").Sky(14f, 0, Side.Left, 0).Parallel(5f)
                    .Hint("SPACE / A : JUMP ONTO THE ROPE").Meadow(4f).Hills(4f, 7f).BigTurn(Side.Right, 40f)
                    .Spiral(Side.Right).Meadow(3f)
                    .Hint("CTRL + TURN : DRIFT").BigTurn(Side.Left, 45f).LaunchRamp().Platforms(4f, 1)
                    .RingArc(5).Hammers(2.5f, 1.8f).Meadow(3f)
                    .FinalRush(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                description = "Hills, a loop and ropes on the city's edge.\nStay on its wheel, take the pads, pass it on the last straight.",
                rival = RivalKind.Moto, rivalPace = 1.05f, theme = Theme.Industrial, seed = 11,
                build = b => b.Start(64f, 2400f)
                    .Road(2f).Sky(16f, 0, Side.Right, 0).Meadow(3f).Parallel(5f).Hills(4f, 9f).BigTurn(Side.Right, 50f)
                    .Hint("LOOPS CARRY YOU : KEEP RUNNING").Loop().Meadow(3f).Lasers(2.5f, 1.4f)
                    .LaunchRamp().Platforms(5f, 1).Spiral(Side.Left)
                    .Sky(12f, 1, Side.Left, 1).Meadow(3f).Parallel(4f).Hills(3f, 9f).RingArc(6)
                    .FinalRush(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE CITY BUS", tagline = "Next stop : your dust.", world = 0,
                description = "Forest hills, a convoy to ride, a rope loop on the side.\nThe bus is clumsy in the turns : drift past it.",
                rival = RivalKind.Bus, rivalPace = 1.08f, theme = Theme.Forest, seed = 22,
                build = b => b.Start(64f, 2600f)
                    .Road(2f).Sky(18f, 0, Side.Right, 2).Meadow(3f).Parallel(5f).Hills(4f, 10f).Convoy(4f, 4)
                    .Hint("CATCH THE ROPE : RIDE THE LOOP").SideLoop(Side.Left)
                    .Spiral(Side.Right).Platforms(5f, 2).Hammers(3f, 1.5f)
                    .Sky(14f, 0, Side.Left, 1).Meadow(3f).Parallel(4f).BigTurn(Side.Right, 60f)
                    .LaunchRamp().Slabs(4f).Crates(3f, 2).Meadow(3f)
                    .RingArc(6).FinalRush(5.5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE SPORTS CAR", tagline = "Zero to sixty in a blink. You blink faster.", world = 0,
                description = "City blocks, a corkscrew and rooftop ropes.\nIt pulls away on the straights : make it up in the air.",
                rival = RivalKind.SportsCar, rivalPace = 1.12f, theme = Theme.City, seed = 33,
                build = b => b.Start(64f, 2800f)
                    .Road(2f).Tunnel(2.5f).Sky(18f, 1, Side.Left, 0).Meadow(3f).Parallel(5f).Lasers(3f, 1.3f).Hills(4f, 10f)
                    .Corkscrew(Side.Left).Meadow(3f).Platforms(5f, 2).Spiral(Side.Left)
                    .Sky(16f, 0, Side.Right, 2).Meadow(3f).Convoy(4f, 5).Parallel(4f).SideLoop(Side.Right)
                    .LaunchRamp().Dive(260f).Meadow(2f)
                    .RingArc(5).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE RACE CAR", tagline = "Built for one thing. So are you.", world = 0,
                description = "Night circuit : a double spiral, a loop, long pad lines.\nEvery pad counts. So does every rope.",
                rival = RivalKind.RaceCar, rivalPace = 1.15f, theme = Theme.NeonNight, seed = 44,
                build = b => b.Start(64f, 3000f)
                    .Road(2f).Sky(20f, 1, Side.Right, 2).Meadow(3f).Parallel(6f).Hills(4f, 10f).Lasers(3f, 1.2f)
                    .SideLoop(Side.Left).Loop().Spiral(Side.Right, 2).Platforms(5f, 2).Hammers(3f, 1.3f)
                    .Sky(16f, 0, Side.Left, 1).Meadow(3f).Parallel(4f).Crates(3f, 4).Hills(3f, 10f)
                    .Twist(2.4f, 1).LaunchRamp().Meadow(2f)
                    .FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "THE BULLET TRAIN", tagline = "Runs on time. You run ahead of it.", world = 1,
                description = "Golden dunes, giant drops, platforms over the void.\nThe train never brakes : you will have to fly.",
                rival = RivalKind.Train, rivalPace = 1.2f, theme = Theme.GoldDunes, seed = 55,
                build = b => b.Start(64f, 3200f)
                    .Road(2f).Sky(22f, 0, Side.Right, 3).Meadow(4f).Parallel(5f).Hills(4f, 12f).Lasers(3f, 1f)
                    .LaunchRamp().Dive(300f).Platforms(5f, 3).Spiral(Side.Left)
                    .Sky(22f, 1, Side.Left, 0).Meadow(3f).Parallel(5f).Hammers(3f, 1.3f).Convoy(4f, 6).Hills(3f, 12f)
                    .SideLoop(Side.Right).Slabs(5f).Corkscrew(Side.Right)
                    .RingArc(6).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FALCONS", tagline = "Fastest thing alive. Until today.", world = 1,
                description = "A desert under an open sky : ropes and rings high in the air.\nThe birds dive. Dive harder.",
                rival = RivalKind.Birds, rivalPace = 1.24f, theme = Theme.Desert, seed = 66,
                build = b => b.Start(64f, 3400f)
                    .Road(2f).Descent(4f, 14f).Sky(24f, 1, Side.Right, 0).Meadow(4f).Parallel(5f).Hills(4f, 12f).Meadow(4f)
                    .Spiral(Side.Right, 2).LaunchRamp().Dive(340f).Platforms(5f, 3)
                    .Sky(22f, 2, Side.Left, 1).Meadow(3f).Parallel(5f).Hammers(3f, 1.2f).Hills(4f, 12f)
                    .SideLoop(Side.Right).Loop(Side.Left)
                    .RingArc(7).Meadow(2f)
                    .FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                description = "A volcano wakes up : lava bombs, hammers, lanes under the plane.\nThe plane is huge. Stay under it.",
                rival = RivalKind.Plane, rivalPace = 1.28f, theme = Theme.Volcano, seed = 77,
                build = b => b.Start(64f, 3600f)
                    .Road(2f).Tunnel(2.5f).Sky(22f, 0, Side.Left, 2).Meadow(3f).Parallel(5f).Hammers(3f, 1.2f).Hills(4f, 12f)
                    .Spiral(Side.Left).Platforms(5f, 3).Crates(3f, 3, true).Loop()
                    .Sky(22f, 1, Side.Right, 0).Meadow(3f).Parallel(5f).Lasers(3f, 1f).Hills(3f, 12f)
                    .SideLoop(Side.Left).LaunchRamp().Slabs(4f).Dive(320f).Meadow(2f)
                    .RingArc(6).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FIGHTER JET", tagline = "Mach two. Mach you.", world = 1,
                description = "A canyon of lasers, meteors and long pad lines.\nChain the pads and the world blurs. Then chain more.",
                rival = RivalKind.FighterJet, rivalPace = 1.32f, theme = Theme.LaserCanyon, seed = 88,
                build = b => b.Start(64f, 3800f)
                    .Road(2f).Sky(24f, 0, Side.Left, 2).Meadow(3f).Parallel(6f).Lasers(3f, 0.9f).Hills(4f, 12f)
                    .Spiral(Side.Right, 2).Meteors(5f, 1.4f).LaunchRamp().Platforms(5f, 3).Loop(Side.Left)
                    .Sky(22f, 1, Side.Right, 0).Meadow(3f).Parallel(5f).Convoy(4f, 6).Hammers(3f, 1.1f).Hills(3f, 12f)
                    .SideLoop(Side.Left).Corkscrew(Side.Left).Meadow(2f)
                    .FinalRush(6.5f).Finish()
            },
            new LevelDefinition
            {
                title = "A SNIPER BULLET", tagline = "It was aimed at nothing. Now it's racing you.", world = 1,
                description = "Ice fields, frozen walls to break, pillars to weave.\nThe bullet only goes straight. You don't have to.",
                rival = RivalKind.Bullet, rivalPace = 1.36f, theme = Theme.Ice, seed = 99,
                build = b => b.Start(64f, 4000f)
                    .Road(2f).Sky(24f, 1, Side.Left, 0).Meadow(3f).Parallel(5f).Pillars(3f, 0.9f).Hills(4f, 12f)
                    .Spiral(Side.Left).SideLoop(Side.Right).LaunchRamp().Platforms(5f, 3).Meteors(4f, 1.4f)
                    .Sky(24f, 0, Side.Right, 2).Meadow(3f).Parallel(5f).Lasers(3f, 1f).Hammers(3f, 1.1f).Hills(3f, 12f)
                    .Loop().Twist(2.4f, 1).Parallel(4f)
                    .FinalRush(6.5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE ROCKETS", tagline = "Three of them. Pick a side.", world = 1,
                description = "Castles in the sky, platforms and ropes. Your dash now breaks blocks.\nThe rockets fly high to the side : stay under them.",
                rival = RivalKind.Rockets, rivalPace = 1.4f, worldBreaker = true, theme = Theme.SkyCastle, seed = 110, music = "music_race2",
                build = b => b.Start(64f, 4200f)
                    .Hint("DASHING BREAKS BLOCKS NOW").Pillars(2f, 0.7f).Sky(28f, 0, Side.Right, 3).Meadow(4f).Parallel(6f).Hills(4f, 12f).Lasers(3f, 0.9f)
                    .SideLoop(Side.Left).Platforms(5f, 3).Spiral(Side.Right, 2).Slabs(4f).Corkscrew()
                    .Sky(26f, 1, Side.Left, 0).Meadow(3f).Parallel(5f).Hammers(3f, 1f).Hills(4f, 12f)
                    .LaunchRamp().Dive(340f).Platforms(4f, 2).RingArc(7)
                    .FinalRush(7f).Finish()
            },

            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "THE STARFIGHTER", tagline = "Lightspeed is a suggestion.", world = 2,
                description = "Open space, huge platforms adrift, a twist and a double spiral.\nHyper all the way : never let it drop.",
                rival = RivalKind.Spaceship, rivalPace = 1.4f, worldBreaker = true, theme = Theme.Space, seed = 121, music = "music_race2",
                build = b => b.Start(64f, 4400f)
                    .Road(2f).Sky(26f, 0, Side.Left, 2).Meadow(3f).Parallel(6f).Lasers(3f, 0.9f).Hills(4f, 12f)
                    .Platforms(6f, 3).Twist(2.4f, 1).Spiral(Side.Left, 2).Loop()
                    .Sky(26f, 1, Side.Right, 0).Meadow(3f).Parallel(5f).Meteors(5f, 1.2f).Hammers(3f, 1f).Hills(3f, 12f)
                    .SideLoop(Side.Right).Slabs(5f).Loop(Side.Left).RingArc(7)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "TIME", tagline = "It never stops. Neither do you.", world = 2,
                description = "The ground gives way behind you.\nDon't stop. Not even once.",
                rival = RivalKind.Time, rivalPace = 1.44f, worldBreaker = true, collapse = true, theme = Theme.Void, seed = 143, music = "music_race2",
                build = b => b.Start(64f, 4800f)
                    .Hint("THE GROUND CANNOT KEEP UP").Road(2f).Sky(26f, 1, Side.Right, 0).Meadow(3f).Parallel(6f).Lasers(3f, 0.9f).Hills(4f, 12f)
                    .Platforms(6f, 3).Corkscrew(Side.Right).Spiral(Side.Right, 2)
                    .Sky(26f, 2, Side.Left, 2).Meadow(3f).Parallel(5f).Hammers(3f, 0.9f).Hills(4f, 12f)
                    .SideLoop(Side.Right).Twist(2.4f, 1).Slabs(5f).LaunchRamp().Dive(360f).Meadow(2f)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "THE T-REX", tagline = "Too fast. You just ran back 66 million years. Something noticed.", world = 2,
                description = "Prehistoric plains, a sky full of meteors.\nIt is behind you. Keep it there.",
                rival = RivalKind.TRex, rivalPace = 0.85f, chaser = true, chaserStart = -420f, worldBreaker = true, collapse = true, theme = Theme.Prehistoric, seed = 154, music = "music_race2",
                build = b => b.Start(64f, 5000f)
                    .Hint("DON'T LET IT CATCH YOU").Road(2f).Sky(28f, 0, Side.Left, 3).Meadow(3f).Parallel(6f).Meteors(6f, 1f).Hills(4f, 12f)
                    .LaunchRamp().Dive(380f).Platforms(6f, 3).Spiral(Side.Left, 2)
                    .Sky(28f, 2, Side.Right, 0).Meadow(3f).Parallel(5f).Hammers(3f, 0.9f).Lasers(3f, 0.9f).Hills(4f, 12f)
                    .SideLoop(Side.Right).Loop().Meteors(5f, 1f).RingArc(7)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "DEATH", tagline = "Don't look back. Seriously.", world = 2,
                description = "A night with no ground left behind, a run upside down.\nChain the pads into light, then never let it go.",
                rival = RivalKind.Death, rivalPace = 0.9f, chaser = true, chaserStart = -460f, worldBreaker = true, collapse = true, theme = Theme.Night, seed = 165, music = "music_race2",
                build = b => b.Start(64f, 5200f)
                    .Hint("DON'T LET DEATH CATCH YOU").Road(2f).Sky(26f, 1, Side.Right, 0).Meadow(3f).Parallel(6f).Hammers(3f, 0.8f).Hills(4f, 12f)
                    .Inverted(3f).Platforms(6f, 3).Spiral(Side.Right, 2).Corkscrew()
                    .Sky(26f, 2, Side.Left, 3).Meadow(3f).Hint("CHAIN THE PADS : TURN TO LIGHT").PadLine(6f, 0.3f).Parallel(6f).Lasers(3f, 0.8f)
                    .SideLoop(Side.Right).LaunchRamp().Slabs(5f).Meadow(2f)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "LIGHT ITSELF", tagline = "Physics called. You declined.", world = 2,
                description = "The void : everything wide open, routes stacked to the sky.\nYou have to become light to keep up.",
                rival = RivalKind.Light, rivalPace = 1.5f, worldBreaker = true, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start(64f, 4600f)
                    .Road(2f).Sky(26f, 1, Side.Left, 2).Meadow(3f).Parallel(6f).Lasers(3f, 0.8f).Hills(4f, 12f)
                    .Platforms(6f, 3).Spiral(Side.Left, 2)
                    .Sky(28f, 2, Side.Right, 0).Meadow(3f).Hint("CHAIN THE PADS : TURN TO LIGHT").PadLine(8f, 0.28f).Parallel(6f).Hammers(3f, 0.8f)
                    .Corkscrew().SideLoop(Side.Right)
                    .Sky(22f, 3, Side.Left, 3).Meadow(3f).Parallel(4f).Hills(4f, 12f).Loop().RingArc(8)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "A SUPERNOVA", tagline = "Outrun the end of the world.", world = 2,
                description = "The last star collapses. The widest, fastest run of all.\nTurn to light and stay light.",
                rival = RivalKind.Supernova, rivalPace = 0.95f, chaser = true, chaserStart = -520f, worldBreaker = true, collapse = true, theme = Theme.Space, seed = 176, music = "music_race2",
                build = b => b.Start(64f, 5400f)
                    .Hint("THE STAR IS COLLAPSING. RUN.").Road(2f).Sky(30f, 0, Side.Left, 3).Meadow(3f).Parallel(6f).Meteors(6f, 0.9f).Hills(4f, 12f)
                    .Platforms(6f, 3).Spiral(Side.Right, 2)
                    .Sky(30f, 2, Side.Right, 0).Meadow(3f).Hint("CHAIN THE PADS : TURN TO LIGHT").PadLine(8f, 0.28f).Parallel(6f).Hammers(3f, 0.8f)
                    .Loop().SideLoop(Side.Left).Slabs(5f).Inverted(3f, Side.Right)
                    .Sky(24f, 1, Side.Left, 1).Meadow(3f).Parallel(5f).Lasers(3f, 0.8f).Meteors(5f, 0.9f).Hills(3f, 12f)
                    .Corkscrew().PadLine(5f, 0.28f).FinalRush(8f).Finish()
            }
        };

        // Every stage's cruise comes from its place in the game (anything reading LevelDefinition.cruise sees it).
        static LevelDatabase()
        {
            for (int i = 0; i < Levels.Length; i++) Levels[i].cruise = CruiseOf(i);
        }

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];

        // 0 on the first stage, 1 on the last.
        public static float Progress01(int pIndex) => Levels.Length <= 1 ? 0f : UnityEngine.Mathf.Clamp01(pIndex / (float)(Levels.Length - 1));

        // The crescendo : each stage about 15 % faster than the one before, ten times faster from 1-1 to 3-6.
        public static float CruiseOf(int pIndex) => SpeedProfile.FIRST_CRUISE * UnityEngine.Mathf.Pow(10f, Progress01(pIndex));

        // Later stages run through their open modules for longer : early stages are short and tight, late ones long.
        public static float Pace(int pIndex) => 1f + 0.35f * Progress01(pIndex);

        // Width on top of the recipe : the stage grows with its speed, ten times from first to last.
        public static float WidthScale(int pIndex) => 0.75f * CruiseOf(pIndex) / SpeedProfile.FIRST_CRUISE;

        // Scattered pads : rare at first so the first stages stay calm, many on the last ones to go much faster.
        public static float PadDensity(int pIndex) => 0.25f + 1.1f * Progress01(pIndex);

        // The stage's recipe, ready to build or to bake into editable data.
        public static CourseBuilder Recipe(int pIndex, UnityEngine.Transform pRoot)
        {
            LevelDefinition lLevel = Get(pIndex);
            SpeedProfile lProfile = Profile(lLevel);
            CourseBuilder lBuilder = new CourseBuilder(pRoot, lLevel.theme(), lProfile, lLevel.seed)
            {
                pace = Pace(pIndex), widthScale = WidthScale(pIndex), padDensity = PadDensity(pIndex), world = lLevel.world,
                size = lProfile.scale, difficulty = Progress01(pIndex)
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

        private static int IndexOf(LevelDefinition pLevel) => Array.IndexOf(Levels, pLevel);

        public static SpeedProfile Profile(LevelDefinition pLevel)
        {
            int lIndex = IndexOf(pLevel);
            pLevel.cruise = lIndex >= 0 ? CruiseOf(lIndex) : pLevel.cruise;
            SpeedProfile lProfile = SpeedProfile.For(pLevel.cruise, pLevel.worldBreaker);
            lProfile.difficulty = lIndex >= 0 ? Progress01(lIndex) : 0.5f;
            return lProfile;
        }

        // The rival's base pace in m/s : a share of the stage's cruise. Rivals then rubber-band around you (Rival.cs) :
        // faster when behind, slower when ahead, gently on the first stages and harder on the last ones.
        public static float RivalSpeed(LevelDefinition pLevel)
        {
            int lIndex = IndexOf(pLevel);
            float lCruise = lIndex >= 0 ? CruiseOf(lIndex) : pLevel.cruise;
            return lCruise * pLevel.rivalPace;
        }
    }
}
