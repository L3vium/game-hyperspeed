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
        public float cruise = 90f;
        public bool worldBreaker;
        public bool collapse;
        public Func<Theme> theme;
        public Action<CourseBuilder> build;
        public int seed;
        public string music = "music_race";
    }

    // 16 stages, each faster than the one before : on foot against vehicles, then against what no vehicle can match.
    // You end up so fast you tear the ground open, turn to light and overtake time itself, which sends you into the past.
    public static class LevelDatabase
    {
        public static readonly string[] WORLDS = { "STREET LEAGUE", "MACHINE LEAGUE", "COSMIC LEAGUE" };

        public static readonly LevelDefinition[] Levels =
        {
            // ---------------------------------------------------------------- STREET LEAGUE
            new LevelDefinition
            {
                title = "THE MOTORBIKE", tagline = "It has an engine. You have legs. Fair.", world = 0,
                rival = RivalKind.Moto, rivalSpeed = 117f, cruise = 90f, theme = Theme.Islands, seed = 11,
                build = b => b.Start(300f, 2400f)
                    .Hint("A / D  MOVE    SHIFT  DASH").Hills(12f, 0.9f)
                    .Hint("SPEED PADS : YOU KEEP ALL THE SPEED").Road(6f).Turn(Side.Right, 60f)
                    .Hint("FALL, THEN S / CTRL IN THE AIR  :  DIVE").Dive().Hills(10f)
                    .Hint("JUMP STRIPS CARRY YOU OVER").Plateaus(3).Hairpin(Side.Left)
                    .Hint("E / CLICK  :  PUNCH THROUGH").Smash(6f, 3).Blocks(10f, 0.8f).Dive(380f)
                    .Hint("THE ROPE PULLS YOU IN").Rope().Hills(10f).Turn(Side.Left, 70f)
                    .Hint("RUN ALONG WALLS  -  RUN AT THEM TO CLIMB").Canyon(8f).Meteors(10f, 3f).Descent(6f, 160f)
                    .Hairpin(Side.Right).SkyRope().Hills(10f).Dive(340f).Road(6f).Finish()
            },
            new LevelDefinition
            {
                title = "THE CITY BUS", tagline = "Next stop : your dust.", world = 0,
                rival = RivalKind.Bus, rivalSpeed = 127f, cruise = 98f, theme = Theme.Islands, seed = 22,
                build = b => b.Start(320f, 2600f)
                    .Hint("CTRL + A / D  :  DRIFT, RELEASE TO BOOST").Hills(10f).Turn(Side.Left, 50f).Blocks(10f, 0.9f).Dive()
                    .Plateaus(3).Hills(8f).Hairpin(Side.Right).Rope().Meteors(10f, 2.8f).Dive(380f).Hills(10f)
                    .Turn(Side.Right, 80f).Canyon(8f).Smash(5f, 3).SkyRope().Descent(6f, 180f).Hairpin(Side.Left)
                    .Blocks(10f, 1f).Hills(8f).Dive(360f).Road(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE SPORTS CAR", tagline = "Zero to sixty in a blink. You blink faster.", world = 0,
                rival = RivalKind.SportsCar, rivalSpeed = 143f, cruise = 110f, theme = Theme.City, seed = 33,
                build = b => b.Start(340f, 2800f)
                    .Road(6f).Blocks(10f, 1f).Turn(Side.Right, 70f).Dive().Canyon(8f).Plateaus(3).Hairpin(Side.Left)
                    .Hills(8f).Rope(3f, 3).Meteors(10f, 2.6f).Dive(400f).Blocks(10f, 1.1f).Turn(Side.Left, 60f)
                    .SkyRope().Canyon(7f).Hairpin(Side.Right).Hills(8f).Dive(360f).Road(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE RACE CAR", tagline = "Built for one thing. So are you.", world = 0,
                rival = RivalKind.RaceCar, rivalSpeed = 162f, cruise = 125f, theme = Theme.City, seed = 44,
                build = b => b.Start(340f, 3000f)
                    .Road(5f).Turn(Side.Left, 80f).Hills(8f).Dive().Blocks(10f, 1.1f).Plateaus(3, 4f).Hairpin(Side.Right)
                    .Canyon(8f).Rope().Meteors(9f, 2.4f).Dive(420f).Hills(8f).Turn(Side.Right, 70f).SkyRope()
                    .Blocks(8f, 1.2f).Hairpin(Side.Left).Descent(5f, 200f).Dive(380f).Road(5f).Finish()
            },

            // ---------------------------------------------------------------- MACHINE LEAGUE
            new LevelDefinition
            {
                title = "THE BULLET TRAIN", tagline = "Runs on time. You run ahead of it.", world = 1,
                rival = RivalKind.Train, rivalSpeed = 182f, cruise = 140f, theme = Theme.Desert, seed = 55,
                build = b => b.Start(360f, 3200f)
                    .Hills(8f).Dive().Plateaus(3, 4f).Turn(Side.Right, 80f).Blocks(9f, 1.2f).Rope(3f, 3).Hairpin(Side.Left)
                    .Meteors(9f, 2.2f).Dive(440f).Canyon(8f).Hills(7f).SkyRope().Turn(Side.Left, 70f).Dive(400f)
                    .Blocks(8f, 1.2f).Road(5f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FALCONS", tagline = "Fastest thing alive. Until today.", world = 1,
                rival = RivalKind.Birds, rivalSpeed = 195f, cruise = 150f, theme = Theme.Desert, seed = 66,
                build = b => b.Start(360f, 3400f)
                    .Descent(5f, 200f).Dive(500f).Hills(8f, 1.2f).SkyRope().Hills(6f, 1.2f).Hairpin(Side.Right).Plateaus(3, 4f)
                    .Dive(460f).Blocks(9f, 1.2f).SkyRope(4f).Turn(Side.Left, 60f).Meteors(8f, 2f).Dive(420f).Canyon(7f)
                    .Hills(7f).Road(4f).Finish()
            },
            new LevelDefinition
            {
                title = "THE AIRLINER", tagline = "Please remain seated. You will not.", world = 1,
                rival = RivalKind.Plane, rivalSpeed = 214f, cruise = 165f, theme = Theme.Industrial, seed = 77,
                build = b => b.Start(380f, 3600f)
                    .Road(4f).Dive(520f).Blocks(9f, 1.3f).Turn(Side.Left, 90f).Plateaus(3, 3.6f).Rope().Hairpin(Side.Right)
                    .Meteors(8f, 2f).Dive(480f).Canyon(7f).Hills(7f, 1.2f).SkyRope().Blocks(8f, 1.3f).Dive(440f).Road(4f).Finish()
            },
            new LevelDefinition
            {
                title = "THE FIGHTER JET", tagline = "Mach two. Mach you.", world = 1,
                rival = RivalKind.FighterJet, rivalSpeed = 240f, cruise = 185f, theme = Theme.Industrial, seed = 88,
                build = b => b.Start(380f, 3800f)
                    .Hills(6f, 1.2f).Dive(540f).Canyon(7f).Plateaus(3, 3.4f).Turn(Side.Right, 90f).Rope(3f, 3).Blocks(9f, 1.3f)
                    .Hairpin(Side.Left).Dive(500f).Meteors(8f, 1.8f).SkyRope().Hills(6f, 1.3f).Dive(460f).Road(4f).Finish()
            },
            new LevelDefinition
            {
                title = "A SNIPER BULLET", tagline = "It was aimed at nothing. Now it's racing you.", world = 1,
                rival = RivalKind.Bullet, rivalSpeed = 266f, cruise = 205f, theme = Theme.Ice, seed = 99,
                build = b => b.Start(400f, 4000f)
                    .Road(3f).Dive(560f).Hills(7f, 1.3f).Hairpin(Side.Right).Blocks(9f, 1.4f).Plateaus(3, 3.2f).Dive(520f)
                    .Meteors(8f, 1.6f).Turn(Side.Left, 70f).Canyon(7f).SkyRope().Dive(480f).Hills(6f, 1.3f).Road(4f).Finish()
            },
            new LevelDefinition
            {
                title = "THE ROCKETS", tagline = "Three of them. Pick a side.", world = 1,
                rival = RivalKind.Rockets, rivalSpeed = 292f, cruise = 225f, worldBreaker = true, theme = Theme.Industrial, seed = 110, music = "music_race2",
                build = b => b.Start(400f, 4200f)
                    .Hint("YOU BREAK THROUGH BLOCKS NOW").Blocks(9f, 1.5f).Dive(580f).Turn(Side.Right, 90f).Meteors(8f, 1.5f).Plateaus(3, 3f)
                    .Rope().Hairpin(Side.Left).Dive(540f).Blocks(8f, 1.6f).SkyRope().Hills(6f, 1.3f).Dive(500f).Road(4f).Finish()
            },

            // ---------------------------------------------------------------- COSMIC LEAGUE
            new LevelDefinition
            {
                title = "THE STARFIGHTER", tagline = "Lightspeed is a suggestion.", world = 2,
                rival = RivalKind.Spaceship, rivalSpeed = 325f, cruise = 250f, worldBreaker = true, theme = Theme.Space, seed = 121, music = "music_race2",
                build = b => b.Start(400f, 4400f)
                    .Hills(6f, 1.4f).Dive(600f).Blocks(9f, 1.6f).Hairpin(Side.Right).Plateaus(3, 3f).Meteors(8f, 1.4f).Dive(560f)
                    .Turn(Side.Left, 80f).Canyon(6f).SkyRope().Dive(520f).Hills(6f, 1.4f).Road(3f).Finish()
            },
            new LevelDefinition
            {
                title = "LIGHT ITSELF", tagline = "Physics called. You declined.", world = 2,
                rival = RivalKind.Light, rivalSpeed = 370f, cruise = 285f, worldBreaker = true, theme = Theme.Void, seed = 132, music = "music_race2",
                build = b => b.Start(420f, 4600f)
                    .Road(3f).Dive(620f).Blocks(8f, 1.7f).Turn(Side.Right, 90f).Meteors(8f, 1.3f).Plateaus(3, 2.8f).Dive(580f)
                    .Hairpin(Side.Left).SkyRope().Hills(6f, 1.4f).Dive(540f).Canyon(6f).Road(3f).Finish()
            },
            new LevelDefinition
            {
                title = "TIME", tagline = "It never stops. Neither do you.", world = 2,
                rival = RivalKind.Time, rivalSpeed = 416f, cruise = 320f, worldBreaker = true, collapse = true, theme = Theme.Void, seed = 143, music = "music_race2",
                build = b => b.Start(420f, 4800f)
                    .Hint("THE GROUND CANNOT KEEP UP").Hills(6f, 1.4f).Dive(640f).Meteors(8f, 1.2f).Hairpin(Side.Right).Blocks(8f, 1.8f).Dive(600f)
                    .Plateaus(3, 2.6f).Turn(Side.Left, 80f).SkyRope().Dive(560f).Hills(6f, 1.4f).Road(3f).Finish()
            },
            new LevelDefinition
            {
                title = "THE T-REX", tagline = "Too fast. You just ran back 66 million years. Something noticed.", world = 2,
                rival = RivalKind.TRex, rivalSpeed = 316f, chaser = true, chaserStart = -500f, cruise = 340f, worldBreaker = true, collapse = true, theme = Theme.Prehistoric, seed = 154, music = "music_race2",
                build = b => b.Start(420f, 5000f)
                    .Hint("DON'T LET IT CATCH YOU").Hills(6f, 1.5f).Dive(620f).Blocks(8f, 1.8f).Turn(Side.Right, 90f).Meteors(8f, 1f).Dive(600f)
                    .Hairpin(Side.Left).Plateaus(3, 2.6f).Hills(6f, 1.5f).Meteors(6f, 0.9f).Dive(560f).Road(3f).Finish()
            },
            new LevelDefinition
            {
                title = "DEATH", tagline = "Don't look back. Seriously.", world = 2,
                rival = RivalKind.Death, rivalSpeed = 353f, chaser = true, chaserStart = -600f, cruise = 380f, worldBreaker = true, collapse = true, theme = Theme.Night, seed = 165, music = "music_race2",
                build = b => b.Start(420f, 5200f)
                    .Hint("DON'T LET DEATH CATCH YOU").Road(3f).Dive(660f).Blocks(8f, 1.9f).Hairpin(Side.Right).Meteors(7f, 1f).Dive(620f)
                    .Canyon(6f).Plateaus(3, 2.5f).Turn(Side.Left, 90f).SkyRope().Dive(580f).Hills(6f, 1.5f).Road(3f).Finish()
            },
            new LevelDefinition
            {
                title = "A SUPERNOVA", tagline = "Outrun the end of the world.", world = 2,
                rival = RivalKind.Supernova, rivalSpeed = 391f, chaser = true, chaserStart = -800f, cruise = 420f, worldBreaker = true, collapse = true, theme = Theme.Space, seed = 176, music = "music_race2",
                build = b => b.Start(420f, 5400f)
                    .Hint("THE STAR IS COLLAPSING. RUN.").Hills(6f, 1.5f).Dive(700f).Meteors(8f, 0.9f).Hairpin(Side.Left).Blocks(8f, 2f).Dive(660f)
                    .Plateaus(3, 2.4f).Turn(Side.Right, 90f).Meteors(7f, 0.8f).SkyRope().Dive(620f).Hills(6f, 1.5f).Road(3f).Finish()
            }
        };

        public static int Count => Levels.Length;

        public static LevelDefinition Get(int pIndex) => Levels[UnityEngine.Mathf.Clamp(pIndex, 0, Levels.Length - 1)];

        // Target : each stage lasts about 3 times what it used to, whatever its speed.
        public static float Pace(int pIndex) => 1.25f + 0.3f * pIndex / (Levels.Length - 1);

        public static SpeedProfile Profile(LevelDefinition pLevel) => SpeedProfile.For(pLevel.cruise, pLevel.worldBreaker);
    }
}
