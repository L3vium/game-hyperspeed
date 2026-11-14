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
                    .Hint("HOLD W / RT TO RUN, LOOK TO STEER").Road(2f).Meadow(5f)
                    .Hint("SHIFT / RB : DASH").BigTurn(Side.Right, 40f)
                    .Sky(16f, 0, Side.Left, 0).Hint("RUN UP THE RAMP : THE SKY ROUTE IS FASTER").Meadow(6f).Hills(4f, 6f).Road(3f)
                    .Hint("CTRL + TURN : DRIFT").BigTurn(Side.Left, 45f)
                    .Hint("PADS IN A ROW RAISE YOUR TOP SPEED").PadLine(3f, 0.4f)
                    .Sky(12f, 0, Side.Right, 1).Meadow(5f).Slabs(3f).Meadow(3f)
                    .RingArc(5).FinalRush(4f).Finish()
            },
            new LevelDefinition
            {
                title = "THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                description = "Hills, ropes and a convoy on the city's edge.\nIt is quick : chain the pads, stay on its wheel, pass it on the last straight.",
                rival = RivalKind.Moto, rivalPace = 1.35f, cruise = 175f, theme = Theme.Industrial, seed = 11,
                build = b => b.Start(64f, 2400f)
                    .Road(2f).Meadow(6f).BigTurn(Side.Right, 60f)
                    .Sky(18f, 0, Side.Right, 0).Meadow(5f).Hills(4f, 8f).BigTurn(Side.Left, 50f)
                    .PadLine(3.5f).Platforms(5f, 1)
                    .Sky(16f, 1, Side.Left, 3).Meadow(6f).Descent(3f, 14f).Road(3f)
                    .Dive(220f).Meadow(3f)
                    .Sky(12f, 0, Side.Right, 1).Hills(4f, 8f).BigTurn(Side.Right, 50f)
                    .RingArc(6).FinalRush(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE CITY BUS", tagline = "Next stop : your dust.", world = 0,
                description = "Forest hills and leaning platforms, a convoy to ride.\nThe bus is clumsy in the turns : drift past it.",
                rival = RivalKind.Bus, rivalPace = 1.35f, cruise = 185f, theme = Theme.Forest, seed = 22,
                build = b => b.Start(66f, 2600f)
                    .Road(2f).Meadow(6f).BigTurn(Side.Left, 70f)
                    .Sky(20f, 0, Side.Right, 2).Meadow(6f).Hint("CATCH THE ROPE : RIDE THE LOOP").SideLoop(Side.Left).Convoy(4f, 4).Hills(4f, 10f)
                    .PadLine(3f).Platforms(5f, 2)
                    .Sky(18f, 1, Side.Left, 0).Meadow(6f).BigTurn(Side.Right, 60f).Slabs(4f)
                    .Crates(3f, 2).Dive(260f).Meadow(3f)
                    .Sky(14f, 0, Side.Right, 1).Hills(4f, 10f).Road(3f).Convoy(3f, 3)
                    .RingArc(6).FinalRush(5.5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE SPORTS CAR", tagline = "Zero to sixty in a blink. You blink faster.", world = 0,
                description = "City blocks, tunnels and rooftop ropes.\nIt pulls away on the straights : make it up in the air.",
                rival = RivalKind.SportsCar, rivalPace = 1.4f, cruise = 200f, theme = Theme.City, seed = 33,
                build = b => b.Start(68f, 2800f)
                    .Road(3f).Meadow(5f).BigTurn(Side.Right, 60f).Tunnel(3f)
                    .Sky(20f, 1, Side.Left, 0).Road(4f).Lasers(3f, 1.4f).Meadow(5f).BigTurn(Side.Left, 50f)
                    .PadLine(3.5f).Platforms(4f, 2)
                    .Sky(16f, 0, Side.Right, 1).Slabs(4f).Convoy(4f, 5).SideLoop(Side.Left).Hills(4f, 10f)
                    .Dive(260f).Meadow(3f).Crates(3f, 3, true)
                    .Sky(14f, 1, Side.Left, 3).Meadow(5f).BigTurn(Side.Right, 50f).Road(3f)
                    .RingArc(5).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE RACE CAR", tagline = "Built for one thing. So are you.", world = 0,
                description = "Night circuit : wall turns, long drifts, a high lane.\nEvery pad counts. So does every rope.",
                rival = RivalKind.RaceCar, rivalPace = 1.42f, cruise = 215f, theme = Theme.NeonNight, seed = 44,
                build = b => b.Start(70f, 3000f)
                    .Road(3f).Meadow(6f).BigTurn(Side.Left, 60f)
                    .Sky(20f, 1, Side.Right, 2).Road(4f).SideLoop(Side.Left).Hills(4f, 10f).BigTurn(Side.Right, 60f)
                    .PadLine(4f).Tunnel(3f)
                    .Loop()
                    .Sky(18f, 0, Side.Left, 0).Meadow(6f).Slabs(5f, 1.1f).Lasers(3f, 1.2f)
                    .PadLine(3f).Platforms(4f, 2)
                    .Sky(14f, 1, Side.Right, 1).Meadow(5f).BigTurn(Side.Left, 50f).Crates(3f, 4)
                    .FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "THE BULLET TRAIN", tagline = "Runs on time. You run ahead of it.", world = 1,
                description = "Golden dunes and giant drops.\nThe train never brakes : you will have to fly.",
                rival = RivalKind.Train, rivalPace = 1.4f, cruise = 235f, theme = Theme.GoldDunes, seed = 55,
                build = b => b.Start(76f, 3200f)
                    .Road(3f).Meadow(6f).BigTurn(Side.Left, 60f)
                    .Sky(22f, 0, Side.Right, 3).Meadow(7f).Hills(4f, 12f).Road(4f)
                    .Dive(300f).Platforms(5f, 3)
                    .Sky(20f, 1, Side.Left, 0).Meadow(6f).SideLoop(Side.Right).BigTurn(Side.Right, 70f).Lasers(3f, 1f)
                    .PadLine(4f).Slabs(5f, 1.1f).Hammers(3f, 1.4f)
                    .Sky(18f, 0, Side.Right, 1).Meadow(6f).Convoy(4f, 6).Hills(4f, 12f)
                    .Dive(320f).RingArc(6).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FALCONS", tagline = "Fastest thing alive. Until today.", world = 1,
                description = "A desert under an open sky : ropes high in the air.\nThe birds dive. Dive harder.",
                rival = RivalKind.Birds, rivalPace = 1.48f, cruise = 255f, theme = Theme.Desert, seed = 66,
                build = b => b.Start(78f, 3400f)
                    .Road(3f).Descent(4f, 14f).Meadow(4f)
                    .Sky(26f, 1, Side.Right, 0).Meadow(8f).BigTurn(Side.Right, 70f).Hills(4f, 12f).Road(4f)
                    .Dive(340f).Platforms(5f, 3)
                    .Sky(24f, 2, Side.Left, 0).Meadow(7f).SideLoop(Side.Right).BigTurn(Side.Left, 60f).Road(4f).Hammers(3f, 1.2f)
                    .RingArc(7).PadLine(4f)
                    .Sky(18f, 1, Side.Right, 1).Meadow(6f).Hills(4f, 12f).Road(4f)
                    .FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                description = "A volcano wakes up : lava bombs, hammers, lanes under the plane.\nThe plane is huge. Stay under it.",
                rival = RivalKind.Plane, rivalPace = 1.5f, cruise = 275f, theme = Theme.Volcano, seed = 77,
                build = b => b.Start(82f, 3600f)
                    .Road(3f).Meadow(6f).Tunnel(3f)
                    .Sky(22f, 0, Side.Left, 2).Hammers(3f, 1.3f).Meadow(6f).BigTurn(Side.Left, 70f).Road(4f)
                    .Platforms(5f, 3).Crates(3f, 3, true)
                    .Loop()
                    .Sky(22f, 1, Side.Right, 0).Meadow(7f).SideLoop(Side.Left).BigTurn(Side.Right, 60f).Slabs(4f)
                    .Dive(320f).PadLine(4f)
                    .Sky(16f, 2, Side.Left, 3).Meadow(6f).Road(4f)
                    .RingArc(6).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FIGHTER JET", tagline = "Mach two. Mach you.", world = 1,
                description = "A canyon of lasers and long pad lines : hyper speed opens here.\nChain five pads and the world blurs. Then chain more.",
                rival = RivalKind.FighterJet, rivalPace = 1.55f, cruise = 295f, hyper = true, theme = Theme.LaserCanyon, seed = 88,
                build = b => b.Start(86f, 3800f)
                    .Road(3f).Meadow(6f).BigTurn(Side.Right, 60f)
                    .Hint("CHAIN FIVE PADS : HYPER SPEED").PadLine(5f, 0.35f)
                    .Sky(24f, 0, Side.Left, 2).Meadow(7f).Lasers(3f, 1f).BigTurn(Side.Left, 70f).Road(4f)
                    .Meteors(5f, 1.6f).Platforms(5f, 3)
                    .Loop(Side.Left)
                    .Sky(22f, 1, Side.Right, 0).Meadow(7f).SideLoop(Side.Left).Convoy(4f, 6).Hills(4f, 12f)
                    .PadLine(4.5f, 0.35f)
                    .Sky(16f, 2, Side.Left, 1).Meadow(6f).BigTurn(Side.Right, 60f)
                    .FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "A SNIPER BULLET", tagline = "It was aimed at nothing. Now it's racing you.", world = 1,
                description = "Ice fields, frozen walls to break, straight long lines.\nThe bullet only goes straight. You don't have to.",
                rival = RivalKind.Bullet, rivalPace = 1.6f, cruise = 315f, hyper = true, theme = Theme.Ice, seed = 99,
                build = b => b.Start(90f, 4000f)
                    .Road(3f).Meadow(6f).BigTurn(Side.Right, 70f)
                    .PadLine(5f, 0.35f).Pillars(4f, 0.9f)
                    .Sky(26f, 1, Side.Left, 0).Meadow(8f).Road(5f).SideLoop(Side.Right).BigTurn(Side.Left, 70f)
                    .Platforms(5f, 3).Meteors(4f, 1.6f)
                    .Sky(24f, 0, Side.Right, 2).Meadow(7f).PadLine(4f, 0.35f).Hills(4f, 12f)
                    .Loop()
                    .Sky(18f, 2, Side.Left, 3).Meadow(6f).Road(4f).SideLoop(Side.Right)
                    .FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE ROCKETS", tagline = "Three of them. Pick a side.", world = 1,
                description = "Castles in the sky, platforms and ropes. Your dash now breaks blocks.\nThe rockets fly high to the side : stay under them.",
                rival = RivalKind.Rockets, rivalPace = 1.58f, cruise = 340f, hyper = true, worldBreaker = true, theme = Theme.SkyCastle, seed = 110, music = "music_race2",
                build = b => b.Start(94f, 4200f)
                    .Hint("DASHING BREAKS BLOCKS NOW").Pillars(2f, 0.7f).Meadow(6f).BigTurn(Side.Left, 70f)
                    .Sky(30f, 0, Side.Right, 3).PadLine(5f, 0.35f).Meadow(8f).Road(5f).SideLoop(Side.Left).Platforms(5f, 3)
                    .Sky(28f, 1, Side.Left, 0).Meadow(8f).BigTurn(Side.Right, 70f).Slabs(4f)
                    .Corkscrew()
                    .Sky(24f, 2, Side.Right, 2).Meadow(7f).PadLine(5f, 0.35f).Road(4f).SideLoop(Side.Left)
                    .Dive(340f).Platforms(4f, 2)
                    .Sky(18f, 3, Side.Left, 1).Meadow(6f).BigTurn(Side.Left, 60f)
                    .FinalRush(6f).Finish()
            },

            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "THE STARFIGHTER", tagline = "Lightspeed is a suggestion.", world = 2,
                description = "Open space, huge platforms adrift.\nHyper all the way : never let the chain drop.",
                rival = RivalKind.Spaceship, rivalPace = 1.6f, cruise = 370f, hyper = true, worldBreaker = true, theme = Theme.Space, seed = 121, music = "music_race2",
                build = b => b.Start(98f, 4400f)
                    .Road(3f).Meadow(7f).BigTurn(Side.Right, 70f)
                    .Sky(26f, 0, Side.Left, 2).PadLine(5f, 0.35f).Platforms(6f, 3).Meadow(6f)
                    .Twist(2.4f, 1)
                    .Sky(28f, 1, Side.Right, 0).Meadow(8f).BigTurn(Side.Left, 70f).Slabs(5f)
                    .Loop()
                    .Sky(26f, 2, Side.Left, 1).Meadow(8f).Road(5f).SideLoop(Side.Right).PadLine(5f, 0.35f)
                    .Sky(20f, 3, Side.Right, 3).Meadow(6f).BigTurn(Side.Right, 60f)
                    .Loop(Side.Left)
                    .RingArc(7).FinalRush(6f).Finish()
            },
            new LevelDefinition
            {
                title = "TIME", tagline = "It never stops. Neither do you.", world = 2,
                description = "The ground gives way behind you.\nDon't stop. Not even once.",
                rival = RivalKind.Time, rivalPace = 1.66f, cruise = 395f, hyper = true, worldBreaker = true, collapse = true, theme = Theme.Void, seed = 143, music = "music_race2",
                build = b => b.Start(102f, 4800f)
                    .Hint("THE GROUND CANNOT KEEP UP").Road(3f).Meadow(7f)
                    .Sky(28f, 1, Side.Right, 0).Platforms(6f, 3).BigTurn(Side.Left, 70f).Meadow(6f)
                    .PadLine(5f, 0.35f)
                    .Corkscrew(Side.Right)
                    .Sky(28f, 2, Side.Left, 2).Meadow(8f).Road(5f).SideLoop(Side.Right).BigTurn(Side.Right, 70f)
                    .Twist(2.4f, 1)
                    .Sky(24f, 0, Side.Right, 3).Meadow(7f).Slabs(5f).PadLine(4f, 0.35f)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "THE T-REX", tagline = "Too fast. You just ran back 66 million years. Something noticed.", world = 2,
                description = "Prehistoric plains, a sky full of meteors.\nIt is behind you. Keep it there.",
                rival = RivalKind.TRex, rivalPace = 0.85f, chaser = true, chaserStart = -420f, cruise = 420f, hyper = true, worldBreaker = true, collapse = true, theme = Theme.Prehistoric, seed = 154, music = "music_race2",
                build = b => b.Start(106f, 5000f)
                    .Hint("DON'T LET IT CATCH YOU").Road(3f).Meadow(7f).BigTurn(Side.Right, 70f)
                    .Meteors(6f, 1.2f).Dive(380f).Platforms(6f, 3)
                    .Sky(30f, 0, Side.Left, 3).Meadow(8f).PadLine(5f, 0.35f).Road(5f).SideLoop(Side.Right)
                    .Sky(30f, 2, Side.Right, 0).Meadow(8f).BigTurn(Side.Left, 70f).Meteors(5f, 1.1f)
                    .Loop()
                    .Sky(24f, 1, Side.Left, 2).Meadow(7f).Road(5f).SideLoop(Side.Right)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "DEATH", tagline = "Don't look back. Seriously.", world = 2,
                description = "A night with no ground left behind. Light speed exists now.\nCharge it with a long chain of pads, then never let it go.",
                rival = RivalKind.Death, rivalPace = 0.9f, chaser = true, chaserStart = -460f, cruise = 445f, hyper = true, lightSpeed = 12000f, lightPads = 24, worldBreaker = true, collapse = true, theme = Theme.Night, seed = 165, music = "music_race2",
                build = b => b.Start(104f, 5200f)
                    .Hint("DON'T LET DEATH CATCH YOU").Road(3f).Meadow(7f).BigTurn(Side.Left, 70f)
                    .Sky(28f, 1, Side.Right, 0).Meadow(8f).Inverted(3f).Platforms(6f, 3)
                    .Sky(28f, 2, Side.Left, 3).Meadow(8f).Road(5f).SideLoop(Side.Right).BigTurn(Side.Right, 70f)
                    .Corkscrew()
                    .Sky(24f, 0, Side.Right, 2).Meadow(7f).Slabs(5f)
                    .Hint("CHAIN THE PADS : CHARGE THE LIGHT").PadLine(12f, 0.3f)
                    .LightHighway(10f).Meadow(4f)
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "LIGHT ITSELF", tagline = "Physics called. You declined.", world = 2,
                description = "The void : almost nothing in the way, everything wide open.\nYou have to become light to keep up.",
                rival = RivalKind.Light, rivalPace = 1.55f, cruise = 470f, hyper = true, lightSpeed = 16000f, lightPads = 32, worldBreaker = true, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start(102f, 4600f)
                    .Road(3f).Meadow(7f).BigTurn(Side.Right, 70f)
                    .Sky(28f, 1, Side.Left, 2).Meadow(8f).Platforms(6f, 3).Road(5f)
                    .Hint("CHAIN THE PADS : CHARGE THE LIGHT").PadLine(15f, 0.3f)
                    .LightHighway(14f)
                    .Sky(30f, 2, Side.Right, 0).Meadow(8f).BigTurn(Side.Left, 70f).Corkscrew()
                    .Sky(24f, 3, Side.Left, 3).Meadow(7f).Road(5f).SideLoop(Side.Right)
                    .Loop()
                    .FinalRush(7f).Finish()
            },
            new LevelDefinition
            {
                title = "A SUPERNOVA", tagline = "Outrun the end of the world.", world = 2,
                description = "The last star collapses. The widest, fastest run of all.\nTurn to light and stay light.",
                rival = RivalKind.Supernova, rivalPace = 0.95f, chaser = true, chaserStart = -520f, cruise = 500f, hyper = true, lightSpeed = 20000f, lightPads = 40, worldBreaker = true, collapse = true, theme = Theme.Space, seed = 176, music = "music_race2",
                build = b => b.Start(110f, 5400f)
                    .Hint("THE STAR IS COLLAPSING. RUN.").Road(3f).Meadow(8f).BigTurn(Side.Right, 70f)
                    .Sky(32f, 0, Side.Left, 3).Meteors(6f, 1f).Platforms(6f, 3).Meadow(8f).Road(5f)
                    .Hint("CHAIN THE PADS : CHARGE THE LIGHT").PadLine(17f, 0.3f)
                    .LightHighway(16f)
                    .Sky(32f, 2, Side.Right, 0).Meadow(8f).BigTurn(Side.Left, 70f).Loop().Road(4f).SideLoop(Side.Left)
                    .Sky(30f, 1, Side.Left, 2).Meteors(5f, 0.9f).Meadow(8f).Slabs(5f)
                    .Inverted(4f, Side.Right)
                    .Sky(24f, 3, Side.Right, 1).Meadow(7f).SideLoop(Side.Left).BigTurn(Side.Right, 60f)
                    .PadLine(8f, 0.3f).FinalRush(8f).Finish()
            }
        };

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];

        // Later stages run through their open modules for longer : they last as long as the early ones despite their speed.
        // Early stages are short and tight, the fast late ones long and huge.
        public static float Pace(int pIndex) => 1f + 0.3f * pIndex / (Levels.Length - 1);

        // Width on top of the recipe : the faster you go, the more room you need to steer.
        public static float WidthScale(int pIndex) => 0.75f + 1.05f * pIndex / (Levels.Length - 1);

        // Scattered pads : rare at first so the first stages stay under control.
        public static float PadDensity(int pIndex) => 0.3f + 0.7f * pIndex / (Levels.Length - 1);

        // The stage's recipe, ready to build or to bake into editable data.
        public static CourseBuilder Recipe(int pIndex, UnityEngine.Transform pRoot)
        {
            LevelDefinition lLevel = Get(pIndex);
            CourseBuilder lBuilder = new CourseBuilder(pRoot, lLevel.theme(), Profile(lLevel), lLevel.seed)
            {
                pace = Pace(pIndex), widthScale = WidthScale(pIndex), padDensity = PadDensity(pIndex), world = lLevel.world
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
