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
        public float rivalPace = 1.35f;
        public float rivalDelay;
        public bool chaser;
        public float chaserStart = -300f;
        // Run speed with no boost at all, in m/s : it grows over the whole game (2026-11-12, the crescendo).
        public float cruise = 120f;
        // Hyper speed exists on this stage (from 2-4 on).
        public bool hyper;
        // Light speed on this stage in m/s (0 : none ; from 3-4 on), and how many chained pads charge it.
        public float lightSpeed;
        public int lightPads = 24;
        public bool worldBreaker;
        public bool collapse;
        public Func<Theme> theme;
        public Action<CourseBuilder> build;
        public int seed;
        public string music = "music_race";
    }

    // 17 stages, each faster than the one before and each with its own idea : on foot against animals and vehicles, then
    // against what no vehicle can match. You end up so fast you tear the ground open, turn to light and overtake time.
    // 2026-11-12 : the dogs open the game ; hyper speed exists from 2-4, light speed from 3-4 ; "Light itself" races you
    // at light speed in 3-5.
    public static class LevelDatabase
    {
        public static readonly string[] WORLDS = { "STREET LEAGUE", "MACHINE LEAGUE", "COSMIC LEAGUE" };

        public static readonly LevelDefinition[] Levels =
        {
            // ---------------------------------------------------------------- STREET LEAGUE
            new LevelDefinition
            {
                title = "THE DOGS", tagline = "Four legs each. You have two. Run anyway.", world = 0,
                description = "A green park valley to learn the run : ropes, a kicker, a few pads.\nThe pack never tires : keep your line and pass it on the last straight.",
                rival = RivalKind.Dogs, rivalPace = 1.2f, cruise = 120f, theme = Theme.Islands, seed = 7,
                build = b => b.Start(60f, 2200f)
                    .Hint("HOLD W / RT TO RUN, LOOK TO STEER").Meadow(3f).BigTurn(Side.Right, 40f)
                    .Hint("SHIFT / RB : DASH").Meadow(2f)
                    .Hint("JUMP ONTO THE ROPE").Ramp().Rope()
                    .Hint("CTRL + TURN : DRIFT").BigTurn(Side.Left, 50f).Hills(2f, 6f)
                    .Hint("PADS IN A ROW RAISE YOUR TOP SPEED").Meadow(2f).PadRun(2f, 0.6f).FinalRush(4f).Finish()
            },
            new LevelDefinition
            {
                title = "THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                description = "Hills, ropes and a convoy on the city's edge.\nIt is quick : chain the pads, stay on its wheel, pass it on the last straight.",
                rival = RivalKind.Moto, rivalPace = 1.35f, cruise = 175f, theme = Theme.Industrial, seed = 11,
                build = b => b.Start(66f, 2400f)
                    .Meadow(4f).BigTurn(Side.Right, 80f).Meadow(3f)
                    .Ramp().Rope()
                    .BigTurn(Side.Left, 110f).Hills(3f, 9f)
                    .Road(2f).Platforms(4f, 1)
                    .Dive(220f).Meadow(3f)
                    .Loop().Ramp().Rope().BigTurn(Side.Right, 90f).Shortcut(4f).Meadow(3f).FinalRush(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE CITY BUS", tagline = "Next stop : your dust.", world = 0,
                description = "Forest hills and leaning platforms, a convoy to ride.\nThe bus is clumsy in the turns : drift past it.",
                rival = RivalKind.Bus, rivalPace = 1.35f, cruise = 185f, theme = Theme.Forest, seed = 22,
                build = b => b.Start(68f, 2600f)
                    .Meadow(3f).BigTurn(Side.Left, 90f).Platforms(5f, 2).Ramp().Rope().Loop(Side.Left).Crates(3f, 2).Dive(260f).Meadow(3f)
                    .Hairpin(Side.Right).RingArc(6).Shortcut(4f).Ramp().Rope().Hills(3f, 12f).BigTurn(Side.Right, 100f)
                    .Convoy(4f, 4).Loop().Ramp().Rope().Slabs(4f, 0.9f).FinalRush(5.5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE SPORTS CAR", tagline = "Zero to sixty in a blink. You blink faster.", world = 0,
                description = "City blocks, tunnels and rooftop ropes.\nIt pulls away on the straights : make it up in the air.",
                rival = RivalKind.SportsCar, rivalPace = 1.4f, cruise = 200f, theme = Theme.City, seed = 33,
                build = b => b.Start(70f, 2800f)
                    .Road(2f).BigTurn(Side.Right, 90f).Tunnel(3f).Ramp().Rope().WallTurn(Side.Right).Lasers(3f, 1.2f).Loop().Platforms(4f, 2).Hairpin(Side.Left)
                    .Slabs(4f).Inverted(2.5f).Ramp().Rope().RingArc(5).Convoy(4f, 5).Dive(260f).Meadow(3f).Shortcut(4f).Crates(3f, 3, true).Twist(3f)
                    .Ramp().Rope().BigTurn(Side.Left, 100f).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE RACE CAR", tagline = "Built for one thing. So are you.", world = 0,
                description = "Night circuit : wall turns, long drifts, a high lane.\nEvery pad counts. So does every rope.",
                rival = RivalKind.RaceCar, rivalPace = 1.42f, cruise = 215f, theme = Theme.NeonNight, seed = 44,
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
                rival = RivalKind.Train, rivalPace = 1.45f, cruise = 235f, theme = Theme.GoldDunes, seed = 55,
                build = b => b.Start(78f, 3200f)
                    .Meadow(3f).Dive(300f).Platforms(5f, 3).BigTurn(Side.Left, 100f).Ramp().Rope().Lasers(3f, 1f).Loop().Hairpin(Side.Left)
                    .Slabs(5f, 1.1f).Hammers(3f, 1.4f).Ramp().Rope().Dive(320f).RingArc(6).Meadow(3f).Shortcut(4f).Twist(3f).Convoy(4f, 6).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FALCONS", tagline = "Fastest thing alive. Until today.", world = 1,
                description = "A desert under an open sky : ropes high in the air.\nThe birds dive. Dive harder.",
                rival = RivalKind.Birds, rivalPace = 1.48f, cruise = 255f, theme = Theme.Desert, seed = 66,
                build = b => b.Start(80f, 3400f)
                    .Descent(3f).Dive(340f).RingArc(7).SkyRope().BigTurn(Side.Right, 100f).Platforms(5f, 3).RingArc(7).Hairpin(Side.Right).SkyRope().Dive(320f)
                    .Meadow(4f).Ramp().Rope().Corkscrew(Side.Right).Shortcut(4f).BigTurn(Side.Left, 90f).Hammers(3f, 1.2f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                description = "A volcano wakes up : lava bombs, hammers, lanes under the plane.\nThe plane is huge. Stay under it.",
                rival = RivalKind.Plane, rivalPace = 1.5f, cruise = 275f, theme = Theme.Volcano, seed = 77,
                build = b => b.Start(84f, 3600f)
                    .Meadow(3f).Tunnel(3f).Hammers(3f, 1.3f).BigTurn(Side.Left, 100f).Ramp().Rope().Inverted(3f).Platforms(5f, 3).Hairpin(Side.Right).Crates(3f, 3, true)
                    .Loop().Corkscrew().Ramp().Rope().Meadow(4f).Shortcut(4f).Dive(320f).BigTurn(Side.Right, 90f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FIGHTER JET", tagline = "Mach two. Mach you.", world = 1,
                description = "A canyon of lasers and long pad lines : hyper speed opens here.\nChain five pads and the world blurs. Then chain more.",
                rival = RivalKind.FighterJet, rivalPace = 1.55f, cruise = 295f, hyper = true, theme = Theme.LaserCanyon, seed = 88,
                build = b => b.Start(88f, 3800f)
                    .Meadow(3f).Twist(2.6f, 2).BigTurn(Side.Right, 100f).Meteors(5f, 1.6f).Corkscrew().Ramp().Rope().Lasers(3f, 1f).Platforms(5f, 3).Loop(Side.Left)
                    .Meadow(4f).Inverted(3f, Side.Right).Convoy(4f, 6).Ramp().Rope().Shortcut(4f).BigTurn(Side.Left, 100f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "A SNIPER BULLET", tagline = "It was aimed at nothing. Now it's racing you.", world = 1,
                description = "Ice fields, frozen walls to break, straight long lines.\nThe bullet only goes straight. You don't have to.",
                rival = RivalKind.Bullet, rivalPace = 1.6f, cruise = 315f, hyper = true, theme = Theme.Ice, seed = 99,
                build = b => b.Start(92f, 4000f)
                    .Road(1.5f).Meadow(4f).BigTurn(Side.Right, 110f).Pillars(4f, 0.9f).Ramp().Rope().Loop().Platforms(5f, 3).Hairpin(Side.Right)
                    .Meteors(4f, 1.6f).Meadow(4f).Ramp().Rope().Shortcut(4f).Corkscrew(Side.Right).BigTurn(Side.Left, 100f).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE ROCKETS", tagline = "Three of them. Pick a side.", world = 1,
                description = "Castles in the sky, platforms and ropes. Your dash now breaks blocks.\nThe rockets fly high to the side : stay under them.",
                rival = RivalKind.Rockets, rivalPace = 1.65f, cruise = 340f, hyper = true, worldBreaker = true, theme = Theme.SkyCastle, seed = 110, music = "music_race2",
                build = b => b.Start(96f, 4200f)
                    .Hint("DASHING BREAKS BLOCKS NOW").Pillars(2f, 0.7f).Meadow(3f).BigTurn(Side.Left, 100f).Platforms(5f, 3).Ramp().Rope().RingArc(7)
                    .Dive(340f).Slabs(4f).Ramp().Rope().Loop().Meadow(3f).Shortcut(4f).BigTurn(Side.Right, 100f).Platforms(4f, 2).Ramp().Rope().FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "THE STARFIGHTER", tagline = "Lightspeed is a suggestion.", world = 2,
                description = "Open space, huge platforms adrift.\nHyper all the way : never let the chain drop.",
                rival = RivalKind.Spaceship, rivalPace = 1.7f, cruise = 370f, hyper = true, worldBreaker = true, theme = Theme.Space, seed = 121, music = "music_race2",
                build = b => b.Start(100f, 4400f)
                    .Meadow(4f).BigTurn(Side.Right, 110f).Ramp().Rope().Platforms(6f, 3).Loop().Meadow(4f).Twist(2.4f, 1)
                    .RingArc(7).Meadow(4f).Shortcut(4f).Ramp().Rope().BigTurn(Side.Left, 100f).Loop(Side.Left).Ramp().Rope().FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "TIME", tagline = "It never stops. Neither do you.", world = 2,
                description = "The ground gives way behind you.\nDon't stop. Not even once.",
                rival = RivalKind.Time, rivalPace = 1.75f, cruise = 395f, hyper = true, worldBreaker = true, collapse = true, theme = Theme.Void, seed = 143, music = "music_race2",
                build = b => b.Start(106f, 4800f)
                    .Hint("THE GROUND CANNOT KEEP UP").Meadow(4f).Platforms(6f, 3).BigTurn(Side.Left, 110f).Ramp().Rope().Loop().Meadow(4f).Corkscrew(Side.Right)
                    .Ramp().Rope().Twist(2.4f, 2).Shortcut(4f).BigTurn(Side.Right, 100f).Ramp().Rope().FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "THE T-REX", tagline = "Too fast. You just ran back 66 million years. Something noticed.", world = 2,
                description = "Prehistoric plains, a sky full of meteors.\nIt is behind you. Keep it there.",
                rival = RivalKind.TRex, rivalPace = 0.85f, chaser = true, chaserStart = -420f, cruise = 420f, hyper = true, worldBreaker = true, collapse = true, theme = Theme.Prehistoric, seed = 154, music = "music_race2",
                build = b => b.Start(110f, 5000f)
                    .Hint("DON'T LET IT CATCH YOU").Meadow(4f).BigTurn(Side.Right, 110f).Meteors(6f, 1.2f).Dive(380f).Platforms(6f, 3).Ramp().Rope().Hairpin(Side.Left)
                    .Meadow(4f).Meteors(5f, 1.1f).Loop().Shortcut(4f).Ramp().Rope().BigTurn(Side.Left, 100f).Ramp().Rope().FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "DEATH", tagline = "Don't look back. Seriously.", world = 2,
                description = "A night with no ground left behind. Light speed exists now.\nCharge it with a long chain of pads, then never let it go.",
                rival = RivalKind.Death, rivalPace = 0.9f, chaser = true, chaserStart = -460f, cruise = 445f, hyper = true, lightSpeed = 12000f, lightPads = 24, worldBreaker = true, collapse = true, theme = Theme.Night, seed = 165, music = "music_race2",
                build = b => b.Start(108f, 5200f)
                    .Hint("DON'T LET DEATH CATCH YOU").Meadow(4f).BigTurn(Side.Left, 110f).Ramp().Rope().Inverted(4f).Corkscrew().Platforms(6f, 3).Hairpin(Side.Right)
                    .Meadow(5f).Ramp().Rope().Twist(2.2f, 2).Shortcut(4f).BigTurn(Side.Right, 110f).Ramp().Rope().PadRun(4f, 0.35f).FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "LIGHT ITSELF", tagline = "Physics called. You declined.", world = 2,
                description = "The void : almost nothing in the way, everything wide open.\nYou have to become light to keep up.",
                rival = RivalKind.Light, rivalPace = 2f, cruise = 470f, hyper = true, lightSpeed = 16000f, lightPads = 32, worldBreaker = true, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start(104f, 4600f)
                    .Road(1.5f).PadRun(4f, 0.4f).Meadow(5f).BigTurn(Side.Right, 110f).RingArc(8).Platforms(6f, 3).Ramp().Rope().Corkscrew().Meadow(5f).Dive(360f)
                    .Shortcut(4f).Ramp().Rope().BigTurn(Side.Left, 110f).Loop().Ramp().Rope().FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "A SUPERNOVA", tagline = "Outrun the end of the world.", world = 2,
                description = "The last star collapses. The widest, fastest run of all.\nTurn to light and stay light.",
                rival = RivalKind.Supernova, rivalPace = 0.95f, chaser = true, chaserStart = -520f, cruise = 500f, hyper = true, lightSpeed = 20000f, lightPads = 40, worldBreaker = true, collapse = true, theme = Theme.Space, seed = 176, music = "music_race2",
                build = b => b.Start(112f, 5400f)
                    .Hint("THE STAR IS COLLAPSING. RUN.").Meadow(4f).Meteors(6f, 1f).Loop().BigTurn(Side.Right, 110f).Platforms(6f, 3).Ramp().Rope().Inverted(4f, Side.Right)
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

        public static SpeedProfile Profile(LevelDefinition pLevel) =>
            SpeedProfile.For(pLevel.cruise, pLevel.worldBreaker, pLevel.hyper, pLevel.lightSpeed, pLevel.lightPads);

        // The rival's base pace in m/s. Rivals run well above your cruise (2026-11-12 : the motorbike was too slow) : only
        // the pads, the ropes and the dashes beat them. Chasers hunt from behind at their own share of your cruise.
        public static float RivalSpeed(LevelDefinition pLevel) => pLevel.cruise * pLevel.rivalPace;
    }
}
