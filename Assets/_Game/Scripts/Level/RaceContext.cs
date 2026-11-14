namespace Com.IsartDigital.Hyperspeed
{
    // Shared references for level objects that need to know about the race.
    public static class RaceContext
    {
        public static PlayerController Player;
        public static LevelLayout Layout;
        public static Theme Theme;
        public static float PlayerProgress;
        public static bool Running;
        // Final sprint : no freeze, no impact frame, nothing that hides the line (2026-11-12).
        public static bool Climax;

        public static void Clear()
        {
            Player = null;
            Layout = null;
            Theme = null;
            PlayerProgress = 0f;
            Running = false;
            Climax = false;
        }
    }
}
