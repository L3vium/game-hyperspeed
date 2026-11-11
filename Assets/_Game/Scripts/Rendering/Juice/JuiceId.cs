namespace Com.IsartDigital.Hyperspeed
{
    // Every juice event of the game : one child GameObject each in the HS_Juice prefab (Resources/Juice), with its own
    // MMF_Player. Values are explicit and only ever appended : the prefab finds its events by these numbers.
    public enum JuiceId
    {
        // ---------------- movement 1xx
        Jump = 100,
        DoubleJump = 101,
        RopeJump = 102,
        Dash = 103,
        DashAir = 104,
        ThrustStart = 105,
        LiftOff = 106,
        Launch = 107,
        Land = 108,
        LandHard = 109,
        DiveLanding = 110,
        DiveStart = 111,
        DriftStart = 112,
        DriftTier = 113,
        RopeCatch = 114,
        RopeExit = 115,
        Punch = 116,
        Flip = 117,
        WallAttach = 118,

        // ---------------- boosts 2xx
        BoostPad = 200,
        Ring = 201,
        DriftBoost = 202,
        BoostOther = 203,

        // ---------------- impacts 3xx
        BreakSmall = 300,
        BreakBig = 301,
        BodyHit = 302,
        PunchHit = 303,
        Crash = 304,
        GlanceOff = 305,
        HazardHit = 306,
        MeteorNear = 307,
        FallingCrash = 308,
        LightningNear = 309,

        // ---------------- speed states 4xx
        HyperEnter = 400,
        HyperExit = 401,
        LightEnter = 402,
        LightExit = 403,

        // ---------------- race 5xx
        CountdownTick = 500,
        Go = 501,
        PerfectStart = 502,
        FinalSprint = 503,
        Overtake = 504,
        RivalOvertakes = 505,
        RivalFinished = 506,
        WrongWay = 507,
        FinishWin = 508,
        FinishLose = 509,
        RankReveal = 510,
        Hint = 511,

        // ---------------- death 6xx
        Death = 600,
        Respawn = 601,

        // ---------------- UI 7xx
        UIMove = 700,
        UISelect = 701,
        UIBack = 702,
        Pause = 703,
        Resume = 704,
        TransitionOut = 705,
        TransitionIn = 706,
    }
}
