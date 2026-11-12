using Com.IsartDigital.Hyperspeed.Feel;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // The starting recipe of every juice event (what the builder puts in a new event). After that the prefab is yours :
    // tune it in the inspector (Assets/_Game/Resources/Juice/HS_Juice.prefab).
    public static class JuiceDefaults
    {
        public const string AUDIO = "Assets/_Game/Resources/Audio/";

        public static string Category(JuiceId pId)
        {
            int lValue = (int)pId;
            if (lValue < 200) return "Movement";
            if (lValue < 300) return "Boost";
            if (lValue < 400) return "Impacts";
            if (lValue < 500) return "Speed";
            if (lValue < 600) return "Race";
            if (lValue < 700) return "Death";
            return "UI";
        }

        public static bool PlaysWhilePaused(JuiceId pId) => (int)pId >= 700;

        public static float MinInterval(JuiceId pId)
        {
            switch (pId)
            {
                case JuiceId.WrongWay: return 1.1f;
                case JuiceId.Land: return 0.12f;
                case JuiceId.GlanceOff: return 0.15f;
                case JuiceId.UIMove: return 0.03f;
                case JuiceId.LightningNear: return 0.3f;
                default: return 0f;
            }
        }

        public static string Notes(JuiceId pId)
        {
            switch (pId)
            {
                case JuiceId.Land: return "Plain landing. Intensity = how hard (0 soft .. 1 very hard).";
                case JuiceId.LandHard: return "Long drop landing. Intensity = impact power 0..1.";
                case JuiceId.DiveLanding: return "Landing out of a dive : a speed kick. Intensity = impact power 0..1.";
                case JuiceId.BoostOther: return "Any other kick (cheats, drift without label...). Intensity = size of the kick 0..1.";
                case JuiceId.DriftTier: return "Drift charge reached a tier. Intensity = tier / 3.";
                case JuiceId.Ring: return "Through a boost ring. Context size = ring radius.";
                case JuiceId.BreakSmall: return "Dashed through a breakable (crate, glass, ice, door, meteor). Context size = its radius. Also a small speed kick.";
                case JuiceId.BreakBig: return "Smashed a monolith (world-breaker stages, light form).";
                case JuiceId.MeteorNear: case JuiceId.FallingCrash: case JuiceId.LightningNear: return "Something exploded or struck nearby. Intensity = 1 close, 0 far.";
                case JuiceId.RankReveal: return "The rank letter crashes on the card. Intensity = rank / 4 (S = 1).";
                default: return "";
            }
        }

        // ---------------------------------------------------------------- recipes

        public static void Fill(JuiceId pId, MMF_Player pPlayer)
        {
            P p = new P(pPlayer);
            Color CYAN = new Color(0.25f, 0.95f, 1f);
            Color GOLD = new Color(1f, 0.82f, 0.2f);
            Color RED = new Color(0.86f, 0.05f, 0.1f);
            Color ORANGE = new Color(1f, 0.6f, 0.09f);
            switch (pId)
            {
                // ---------------- movement
                case JuiceId.Jump:
                    p.Sounds(0.8f, "sfx_jump_1", "sfx_jump_2", "sfx_jump_3").PitchFromIntensity = 0.1f;
                    p.FX(HSFXKind.Puff, HSColorSource.ThemeDust, Color.white, 8, 10f, 20f, 0.4f, HSDirection.ContextNormal);
                    p.Body(3.2f, 0f, 0f);
                    p.Fov(5f);
                    p.Shake(0.08f);
                    p.Lines(0.25f);
                    break;
                case JuiceId.DoubleJump:
                    p.Sound("sfx_doublejump", 0.9f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 18f, 0f, 0.3f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 30, 6f, 30f, 0.35f, HSDirection.ContextNormal);
                    p.Body(2.6f, 0f, 0.35f);
                    p.Fov(8f);
                    p.Rollback(5f);
                    p.Shake(0.14f);
                    p.Screen(0.8f, 0.4f, 0.4f, 0.1f, 0.1f, 10f, 0.3f);
                    break;
                case JuiceId.RopeJump:
                    p.Sound("sfx_jump", 0.9f).Pitch = 1.1f;
                    p.Sound("sfx_thunder", 0.4f).Pitch = 1.5f;
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 60, 8f, 45f, 0.4f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, CYAN, 1, 24f, 0f, 0.3f, HSDirection.ContextNormal);
                    p.Fov(10f);
                    p.Rollback(8f);
                    p.Shake(0.22f);
                    break;
                case JuiceId.Dash:
                case JuiceId.DashAir:
                    // 2026-11-12 : no freeze, no impact frame, no full-screen flash on routine actions : the kick is told
                    // by the lines, the edges, a short FOV punch, the shock in the world and the sound.
                    p.Sounds(1f, "sfx_dash_1", "sfx_dash_2", "sfx_dash_3");
                    p.Fov(12f);
                    p.Rollback(10f);
                    p.Punch(new Vector3(-1.5f, 0f, 2.5f));
                    p.Shake(0.18f);
                    p.Screen(1.4f, 0.8f, 0.6f, 0.1f, 0.2f, 12f, 0.4f);
                    p.Vignette(Color.white, 0.45f);
                    p.Lines(1f);
                    p.Body(0f, 3.4f, 0.6f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 26f, 0f, 0.28f, HSDirection.ContextDirection);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 50, 8f, 55f, 0.35f, HSDirection.ContextDirection);
                    if (pId == JuiceId.Dash) p.FX(HSFXKind.Puff, HSColorSource.ThemeDust, Color.white, 14, 14f, 40f, 0.4f, HSDirection.ContextNormal);
                    else p.FX(HSFXKind.Burst, HSColorSource.Fixed, CYAN, 1, 26f, 0f, 0.3f, HSDirection.ContextDirection);
                    p.Rumble(0.55f);
                    break;
                case JuiceId.ThrustStart:
                    p.Sound("sfx_boost_small", 0.8f);
                    p.Fov(12f);
                    p.Rollback(10f);
                    p.Screen(1.2f, 0.8f, 0.5f, 0.1f, 0.15f, 10f, 0.5f);
                    p.Lines(0.7f);
                    p.Shake(0.16f);
                    break;
                case JuiceId.LiftOff:
                    p.Sound("sfx_launch", 0.85f);
                    p.Fov(12f);
                    p.Rollback(12f);
                    p.Shake(0.28f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.ThemeDust, Color.white, 1, 34f, 0f, 0.35f, HSDirection.ContextNormal);
                    break;
                case JuiceId.Launch:
                    p.Sound("sfx_launch", 1f);
                    p.Fov(12f);
                    p.Rollback(10f);
                    p.Shake(0.2f);
                    p.Screen(1.4f, 0.9f, 0.6f, 0.15f, 0.2f, 15f, 0.5f);
                    p.Lines(0.9f);
                    p.Body(3.4f, 0f, 0.4f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 44f, 0f, 0.35f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, GOLD, 70, 8f, 50f, 0.45f, HSDirection.ContextNormal);
                    break;
                case JuiceId.Land:
                    p.Sounds(0.6f, "sfx_land_1", "sfx_land_2", "sfx_land_3").IntensityRemap = new Vector2(0.6f, 1.2f);
                    p.FX(HSFXKind.Puff, HSColorSource.ThemeDust, Color.white, 8, 14f, 25f, 0.4f, HSDirection.ContextNormal);
                    p.Body(-1.6f, 0f, 0f).IntensityRemap = new Vector2(0.4f, 1.6f);
                    p.Shake(0.06f).IntensityRemap = new Vector2(0.3f, 2f);
                    break;
                case JuiceId.LandHard:
                    p.Sound("sfx_land_heavy", 0.95f);
                    p.Shake(0.3f).IntensityRemap = new Vector2(0.4f, 1.5f);
                    p.Punch(new Vector3(5f, 0f, 2f)).IntensityRemap = new Vector2(0.4f, 1.4f);
                    p.Fov(8f);
                    p.Screen(1f, 0.5f, 0.5f, 0.1f, 0.15f, 0f, 0.35f);
                    p.Body(-2.8f, 0f, 0.2f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 60f, 0f, 0.4f, HSDirection.ContextNormal).IntensityRemap = new Vector2(0.35f, 1.4f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.ThemeDust, Color.white, 1, 36f, 0f, 0.3f, HSDirection.ContextNormal).IntensityRemap = new Vector2(0.35f, 1.4f);
                    p.FX(HSFXKind.Shards, HSColorSource.ThemeRoadSide, Color.gray, 36, 26f, 60f, 0.5f, HSDirection.ContextNormal).IntensityRemap = new Vector2(0.4f, 1.4f);
                    p.FX(HSFXKind.Puff, HSColorSource.ThemeDust, Color.white, 22, 60f, 90f, 0.5f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Sparks, HSColorSource.ThemeDust, Color.white, 60, 8f, 70f, 0.5f, HSDirection.ContextNormal);
                    p.Rumble(0.6f).IntensityRemap = new Vector2(0.4f, 1.2f);
                    break;
                case JuiceId.DiveLanding:
                    p.Sound("sfx_land_perfect", 1f);
                    p.Sound("sfx_boost", 0.7f).Pitch = 0.9f;
                    p.Fov(14f);
                    p.Rollback(14f);
                    p.Punch(new Vector3(4f, 0f, 2f));
                    p.Shake(0.35f);
                    p.Vignette(CYAN, 0.7f);
                    p.Screen(1.8f, 1.1f, 0.9f, 0.15f, 0.25f, 18f, 0.5f);
                    p.Lines(1f);
                    p.Body(-2.8f, 3f, 0.6f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 95f, 0f, 0.45f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, CYAN, 1, 64f, 0f, 0.55f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Shards, HSColorSource.ThemeRoadSide, Color.gray, 50, 30f, 70f, 0.5f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 90, 9f, 80f, 0.5f, HSDirection.ContextNormal);
                    p.Rumble(0.75f);
                    p.Duck(0.3f);
                    break;
                case JuiceId.DiveStart:
                    p.Sound("sfx_dive", 0.8f);
                    p.Fov(8f);
                    p.Lines(0.5f);
                    break;
                case JuiceId.DriftStart:
                    p.Sound("sfx_drift_start", 0.75f);
                    p.Shake(0.08f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, GOLD, 24, 6f, 30f, 0.35f, HSDirection.ContextNormal);
                    break;
                case JuiceId.DriftTier:
                    p.Sound("sfx_drift_tier", 0.85f).PitchFromIntensity = 0.4f;
                    p.Screen(0.6f, 0.2f, 0.4f, 0.05f, 0f, 10f, 0.3f).IntensityRemap = new Vector2(0.5f, 1.5f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, GOLD, 40, 7f, 40f, 0.4f, HSDirection.ContextNormal);
                    p.Shake(0.12f);
                    break;
                case JuiceId.RopeCatch:
                    p.Sound("sfx_grind_start", 1f);
                    p.Sound("sfx_thunder", 0.55f).Pitch = 1.4f;
                    p.Fov(10f);
                    p.Rollback(8f);
                    p.Shake(0.15f);
                    p.Vignette(CYAN, 0.6f);
                    p.Screen(1.4f, 0.7f, 1.1f, 0.1f, 0.15f, 15f, 0.45f);
                    p.Lines(0.8f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 110, 9f, 70f, 0.45f, HSDirection.Random);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, CYAN, 1, 46f, 0f, 0.35f, HSDirection.ContextDirection);
                    p.Rumble(0.5f);
                    break;
                case JuiceId.RopeExit:
                    p.Sound("sfx_rope_exit", 1f);
                    p.Fov(12f);
                    p.Rollback(10f);
                    p.Shake(0.2f);
                    p.Vignette(CYAN, 0.5f);
                    p.Screen(2.2f, 1.2f, 1.2f, 0.25f, 0.3f, 20f, 0.55f);
                    p.Lines(1f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, CYAN, 1, 60f, 0f, 0.4f, HSDirection.ContextDirection);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 80, 9f, 70f, 0.45f, HSDirection.Random);
                    break;
                case JuiceId.Punch:
                    p.Sound("sfx_punch_whoosh", 0.8f);
                    p.Body(0f, 4f, 0f);
                    break;
                case JuiceId.Flip:
                    p.Sounds(0.6f, "sfx_flip_1", "sfx_flip_2", "sfx_flip_3");
                    break;
                case JuiceId.WallAttach:
                    p.Sound("sfx_wall_attach", 0.7f);
                    p.Shake(0.1f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 24, 6f, 30f, 0.35f, HSDirection.ContextNormal);
                    break;

                // ---------------- boosts
                case JuiceId.BoostPad:
                    p.Sound("sfx_pad", 1f);
                    p.Sound("sfx_impact", 0.45f);
                    p.Fov(14f);
                    p.Rollback(16f);
                    p.Punch(new Vector3(-3f, 0f, 3f));
                    p.Shake(0.25f);
                    p.Vignette(CYAN, 1f);
                    p.Screen(2.2f, 1.4f, 1f, 0.25f, 0.3f, 22f, 0.6f);
                    p.Lines(1f);
                    p.Body(0f, 4f, 0.8f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, CYAN, 1, 72f, 0f, 0.45f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 40f, 0f, 0.3f, HSDirection.ContextDirection);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 120, 10f, 70f, 0.5f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Burst, HSColorSource.Fixed, CYAN, 1, 40f, 0f, 0.3f, HSDirection.ContextDirection);
                    p.Rumble(0.8f);
                    p.Duck(0.3f);
                    break;
                case JuiceId.Ring:
                    p.Sound("sfx_ring", 1f);
                    p.Sound("sfx_boost", 0.6f).Pitch = 1.15f;
                    p.Fov(12f);
                    p.Rollback(10f);
                    p.Shake(0.15f);
                    p.Vignette(GOLD, 0.7f);
                    p.Screen(2f, 1.2f, 1.2f, 0.25f, 0.25f, 20f, 0.6f);
                    p.Lines(1f);
                    p.Body(0f, 3f, 0.6f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 2.6f, 0f, 0.35f, HSDirection.ContextDirection).UseContextSize = true;
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, GOLD, 90, 10f, 70f, 0.5f, HSDirection.Random);
                    p.Rumble(0.5f);
                    break;
                case JuiceId.DriftBoost:
                    p.Sound("sfx_boost", 0.95f);
                    p.Fov(18f);
                    p.Rollback(18f);
                    p.Shake(0.2f);
                    p.Vignette(GOLD, 0.6f);
                    p.Screen(1.6f, 0.9f, 0.9f, 0.15f, 0.2f, 18f, 0.5f);
                    p.Lines(1f);
                    p.Body(0f, 3f, 0.5f);
                    p.FX(HSFXKind.Burst, HSColorSource.Fixed, GOLD, 1, 30f, 0f, 0.3f, HSDirection.ContextDirection);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, GOLD, 70, 8f, 60f, 0.45f, HSDirection.ContextNormal);
                    p.Popup("{label}", GOLD);
                    break;
                case JuiceId.BoostOther:
                    p.Sound("sfx_boost", 0.85f);
                    p.Fov(16f).IntensityRemap = new Vector2(0.4f, 1f);
                    p.Rollback(16f).IntensityRemap = new Vector2(0.4f, 1f);
                    p.Shake(0.25f).IntensityRemap = new Vector2(0.4f, 1f);
                    p.Screen(1.6f, 0.9f, 0.8f, 0.15f, 0.2f, 15f, 0.45f).IntensityRemap = new Vector2(0.4f, 1f);
                    p.Vignette(CYAN, 0.5f);
                    p.Lines(0.8f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 60, 9f, 50f, 0.45f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Burst, HSColorSource.Fixed, CYAN, 1, 24f, 0f, 0.3f, HSDirection.ContextDirection);
                    p.Popup("{label}", CYAN);
                    break;

                // ---------------- impacts
                case JuiceId.BreakSmall:
                case JuiceId.BreakBig:
                    bool lBig = pId == JuiceId.BreakBig;
                    // A tiny catch on the big ones only ; never an impact frame (they hid the track ahead).
                    if (lBig) p.Hitstop(0.03f);
                    p.Sound("sfx_punch_hit", 0.8f);
                    p.SoundId("sfx_break_{label}", 1f);
                    if (lBig) p.Sound("sfx_explosion", 0.8f);
                    p.Shake(lBig ? 0.45f : 0.3f);
                    p.Punch(new Vector3(-2.5f, 1f, 4f));
                    p.Fov(lBig ? 14f : 10f);
                    p.Rollback(lBig ? 12f : 8f);
                    p.Vignette(Color.white, lBig ? 0.6f : 0.4f);
                    p.Screen(2.6f, 1.3f, 1.1f, 0.35f, 0.3f, 25f, 0.5f);
                    p.Lines(0.9f);
                    p.Body(0f, 3f, 0.6f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 3f, 0f, 0.3f, HSDirection.TowardCamera).UseContextSize = true;
                    p.FX(HSFXKind.Shockwave, HSColorSource.ThemeAccent, Color.white, 1, 4.5f, 0f, 0.45f, HSDirection.ContextNormal).UseContextSize = true;
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 110, 10f, 80f, 0.5f, HSDirection.Random);
                    p.FX(HSFXKind.Burst, HSColorSource.ThemeAccent, Color.white, 1, 46f, 0f, 0.3f, HSDirection.ContextDirection);
                    p.Rumble(lBig ? 1f : 0.7f);
                    p.Duck(0.25f);
                    break;
                case JuiceId.BodyHit:
                    p.Sound("sfx_crash", 0.75f).Pitch = 1.2f;
                    p.Shake(0.3f);
                    p.Punch(new Vector3(4f, 0f, -4f));
                    p.Vignette(RED, 0.6f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 30, 6f, 30f, 0.35f, HSDirection.Random);
                    break;
                case JuiceId.PunchHit:
                    p.Hitstop(0.025f);
                    p.Sound("sfx_punch_hit", 1f);
                    p.Shake(0.5f);
                    p.FX(HSFXKind.Burst, HSColorSource.Fixed, ORANGE, 1, 40f, 0f, 0.3f, HSDirection.ContextDirection);
                    break;
                case JuiceId.Crash:
                    p.Hitstop(0.03f);
                    p.Sound("sfx_crash", 1f);
                    p.Shake(0.6f);
                    p.Punch(new Vector3(6f, 3f, -7f));
                    p.Vignette(RED, 1f);
                    p.Impact(ImpactReason.Big, false, 1, 0.6f);
                    p.Screen(2f, 0.6f, 0.4f, 0f, 0.2f, -40f, 0.45f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 50, 7f, 40f, 0.4f, HSDirection.Random);
                    p.Rumble(1f);
                    break;
                case JuiceId.GlanceOff:
                    p.Sound("sfx_crash", 0.35f).Pitch = 1.5f;
                    p.Shake(0.12f);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 14, 5f, 25f, 0.3f, HSDirection.ContextNormal);
                    break;
                case JuiceId.HazardHit:
                    p.Sound("sfx_hammer_hit", 1f);
                    p.Shake(0.4f);
                    p.Vignette(RED, 0.7f);
                    break;
                case JuiceId.MeteorNear:
                    p.Sound("sfx_explosion", 1f).IntensityRemap = new Vector2(0.3f, 1f);
                    p.Shake(0.7f).IntensityRemap = new Vector2(0f, 1f);
                    p.Vignette(ORANGE, 0.8f).IntensityRemap = new Vector2(0f, 1f);
                    p.Screen(1.5f, 0.6f, 0.8f, 0.15f, 0.15f, 10f, 0.4f).IntensityRemap = new Vector2(0f, 1f);
                    p.Rumble(0.6f).IntensityRemap = new Vector2(0f, 1f);
                    break;
                case JuiceId.FallingCrash:
                    p.Sound("sfx_vehicle_crash", 0.9f).IntensityRemap = new Vector2(0.3f, 1f);
                    p.Sound("sfx_rock", 0.7f).IntensityRemap = new Vector2(0.3f, 1f);
                    p.Shake(0.55f).IntensityRemap = new Vector2(0f, 1f);
                    p.Rumble(0.5f).IntensityRemap = new Vector2(0f, 1f);
                    break;
                case JuiceId.LightningNear:
                    p.Sound("sfx_thunder", 1f).IntensityRemap = new Vector2(0.4f, 1f);
                    p.Flash(Color.white, 0.2f).IntensityRemap = new Vector2(0f, 1f);
                    p.Shake(0.2f).IntensityRemap = new Vector2(0f, 1f);
                    break;

                // ---------------- speed states
                case JuiceId.HyperEnter:
                    p.Sound("sfx_hyper", 1f);
                    p.Sound("sfx_impact", 0.5f);
                    p.Vignette(CYAN, 1f);
                    p.Fov(16f);
                    p.Rollback(20f);
                    p.Punch(new Vector3(-3f, 0f, 4f));
                    p.Shake(0.35f);
                    p.Screen(2.6f, 1.6f, 1.4f, 0.3f, 0.4f, 30f, 0.9f);
                    p.Lines(1f);
                    p.Body(0f, 4f, 1f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, CYAN, 1, 130f, 0f, 0.6f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 80f, 0f, 0.4f, HSDirection.ContextDirection);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 200, 12f, 110f, 0.6f, HSDirection.Random);
                    p.Rumble(1f);
                    p.Duck(0.5f);
                    break;
                case JuiceId.HyperExit:
                    p.Sound("sfx_hyper_out", 0.75f);
                    p.Screen(0.8f, 0.3f, 0.3f, 0f, 0.1f, -10f, 0.4f);
                    p.Shake(0.15f);
                    break;
                case JuiceId.LightEnter:
                    // The one moment that still gets an impact frame : entering light (one frame ; it also hides the
                    // switch to the wide view).
                    p.Sound("sfx_light", 1f);
                    p.Sound("sfx_impact", 0.7f);
                    p.Flash(Color.white, 0.35f);
                    p.Impact(ImpactReason.LightSpeed, false, 1, 0.6f);
                    p.Fov(20f);
                    p.Rollback(26f);
                    p.Shake(0.5f);
                    p.Screen(5f, 3f, 3f, 1f, 0.7f, 50f, 1.6f);
                    p.Lines(1f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 220f, 0f, 0.8f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Burst, HSColorSource.Fixed, Color.white, 1, 60f, 0f, 0.4f, HSDirection.ContextDirection);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, Color.white, 300, 14f, 140f, 0.7f, HSDirection.Random);
                    p.Rumble(1f);
                    p.Duck(0.8f);
                    break;
                case JuiceId.LightExit:
                    p.Sound("sfx_hyper_out", 0.85f).Pitch = 1.2f;
                    p.Screen(1f, 0.4f, 0.4f, 0f, 0.15f, -10f, 0.5f);
                    p.Shake(0.2f);
                    break;

                // ---------------- race
                case JuiceId.CountdownTick:
                    p.Sound("sfx_count", 1f);
                    p.Shake(0.15f);
                    p.Fov(4f);
                    p.Punch(new Vector3(2f, 0f, 0f));
                    break;
                case JuiceId.Go:
                    p.Sound("sfx_go", 1f);
                    p.Shake(0.25f);
                    p.Fov(10f);
                    p.Rollback(8f);
                    p.Vignette(Color.white, 0.5f);
                    p.Screen(1.5f, 0.8f, 0.8f, 0.15f, 0.2f, 20f, 0.5f);
                    p.Lines(0.8f);
                    break;
                case JuiceId.PerfectStart:
                    p.Sound("sfx_perfect_start", 1f);
                    p.Sound("sfx_boost", 0.8f);
                    p.Fov(14f);
                    p.Rollback(14f);
                    p.Shake(0.25f);
                    p.Vignette(GOLD, 0.9f);
                    p.Screen(1.8f, 1.1f, 1f, 0.15f, 0.25f, 20f, 0.6f);
                    p.Lines(1f);
                    break;
                case JuiceId.FinalSprint:
                    p.Sound("ui_hint", 1f).Pitch = 0.8f;
                    p.Screen(1.2f, 0.5f, 0.8f, 0.1f, 0.1f, 30f, 1f);
                    p.Shake(0.3f);
                    p.Fov(10f);
                    break;
                case JuiceId.Overtake:
                    p.Sound("sfx_pass", 1f);
                    p.Rollback(10f);
                    p.Fov(8f);
                    p.Shake(0.15f);
                    p.Screen(0.8f, 0.4f, 0.4f, 0.05f, 0.1f, 10f, 0.4f);
                    break;
                case JuiceId.RivalOvertakes:
                    p.Sound("sfx_pass", 0.8f).Pitch = 0.8f;
                    p.Vignette(RED, 0.5f);
                    p.Shake(0.1f);
                    break;
                case JuiceId.RivalFinished:
                    p.Sound("sfx_rival_win", 1f);
                    p.Vignette(RED, 1f);
                    p.Screen(0.8f, 0.2f, 0.2f, 0f, 0.1f, -20f, 0.8f);
                    p.Shake(0.25f);
                    break;
                case JuiceId.WrongWay:
                    p.Sound("sfx_wrong_way", 0.8f);
                    break;
                case JuiceId.FinishWin:
                    p.Sound("sfx_finish", 1f);
                    p.SlowMo(0.55f, 0.6f, 0f);
                    p.Vignette(GOLD, 1f);
                    p.Fov(12f);
                    p.Shake(0.3f);
                    p.Screen(3f, 1f, 2f, 0.4f, 0.3f, 40f, 1.5f);
                    p.Rumble(1f);
                    break;
                case JuiceId.FinishLose:
                    p.Vignette(RED, 1f);
                    p.Shake(0.3f);
                    p.Sound("sfx_lose", 1f);
                    p.SlowMo(0.6f, 0.5f, 0f);
                    p.Screen(1.5f, 0.3f, 0.2f, -0.2f, 0.1f, -60f, 1.2f);
                    break;
                case JuiceId.RankReveal:
                    p.SoundId("sfx_rank_{label}", 1f);
                    p.Shake(0.4f).IntensityRemap = new Vector2(0.5f, 1.2f);
                    p.Vignette(GOLD, 0.6f);
                    p.Screen(1.4f, 0.4f, 0.8f, 0.1f, 0.2f, 20f, 0.5f);
                    p.Rumble(0.6f);
                    break;
                case JuiceId.Hint:
                    p.Sound("ui_hint", 0.5f);
                    break;

                // ---------------- death
                case JuiceId.Death:
                    p.Hitstop(0.03f);
                    p.Sound("sfx_death", 1f);
                    p.Flash(RED, 0.25f);
                    p.Impact(ImpactReason.Big, false, 1, 0.6f);
                    p.Shake(0.7f);
                    p.Screen(2.5f, 0.4f, 0.3f, -0.1f, 0.2f, -60f, 0.8f);
                    p.Rumble(0.8f);
                    break;
                case JuiceId.Respawn:
                    p.Sound("sfx_respawn", 0.9f);
                    p.Flash(Color.white, 0.2f);
                    p.Screen(1.2f, 0.4f, 1.5f, 0.2f, 0.1f, 20f, 0.6f);
                    p.Body(3f, 0f, 1f);
                    p.FX(HSFXKind.Shockwave, HSColorSource.Fixed, Color.white, 1, 30f, 0f, 0.35f, HSDirection.ContextNormal);
                    p.FX(HSFXKind.Sparks, HSColorSource.Fixed, CYAN, 80, 8f, 50f, 0.45f, HSDirection.Random);
                    break;

                // ---------------- UI
                case JuiceId.UIMove: p.Sound("ui_move", 0.5f); break;
                case JuiceId.UISelect: p.Sound("ui_select", 0.8f); break;
                case JuiceId.UIBack: p.Sound("ui_back", 0.7f); break;
                case JuiceId.Pause: p.Sound("ui_select", 0.7f); break;
                case JuiceId.Resume: p.Sound("ui_back", 0.6f); break;
                case JuiceId.TransitionOut: p.Sound("ui_transition_in", 0.9f); break;
                case JuiceId.TransitionIn: p.Sound("ui_transition_out", 0.85f); break;
            }
        }

        // ---------------------------------------------------------------- recipe helpers

        private class P
        {
            private readonly MMF_Player _player;

            public P(MMF_Player pPlayer) { _player = pPlayer; }

            private T Add<T>(string pLabel) where T : MMF_Feedback
            {
                T lFeedback = (T)_player.AddFeedback(typeof(T));
                lFeedback.Label = pLabel;
                return lFeedback;
            }

            public MMF_HSSound Sound(string pId, float pVolume)
            {
                MMF_HSSound lSound = Add<MMF_HSSound>("Sound " + pId);
                AudioClip lClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO + pId + ".wav");
                if (lClip != null) lSound.Clips = new[] { lClip };
                else lSound.ClipId = pId;
                lSound.Volume = pVolume;
                return lSound;
            }

            // Several takes played in turn : the same action never sounds twice the same.
            public MMF_HSSound Sounds(float pVolume, params string[] pIds)
            {
                MMF_HSSound lSound = Add<MMF_HSSound>("Sound " + pIds[0] + " (+" + (pIds.Length - 1) + ")");
                System.Collections.Generic.List<AudioClip> lClips = new System.Collections.Generic.List<AudioClip>();
                foreach (string lId in pIds)
                {
                    AudioClip lClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO + lId + ".wav");
                    if (lClip != null) lClips.Add(lClip);
                }
                if (lClips.Count > 0) lSound.Clips = lClips.ToArray();
                else lSound.ClipId = pIds[0];
                lSound.Volume = pVolume;
                return lSound;
            }

            // A clip chosen by name when it plays : {label} is the event's label.
            public MMF_HSSound SoundId(string pId, float pVolume)
            {
                MMF_HSSound lSound = Add<MMF_HSSound>("Sound " + pId);
                lSound.ClipId = pId;
                lSound.Volume = pVolume;
                return lSound;
            }

            public MMF_HSShake Shake(float pTrauma)
            {
                MMF_HSShake lShake = Add<MMF_HSShake>("Shake");
                lShake.Trauma = pTrauma;
                return lShake;
            }

            public MMF_HSFovKick Fov(float pDegrees)
            {
                MMF_HSFovKick lFov = Add<MMF_HSFovKick>("FOV kick");
                lFov.Degrees = pDegrees;
                return lFov;
            }

            public MMF_HSRollback Rollback(float pMeters)
            {
                MMF_HSRollback lBack = Add<MMF_HSRollback>("Camera pull back");
                lBack.Meters = pMeters;
                return lBack;
            }

            public MMF_HSCameraPunch Punch(Vector3 pDegrees)
            {
                MMF_HSCameraPunch lPunch = Add<MMF_HSCameraPunch>("Camera punch");
                lPunch.Degrees = pDegrees;
                return lPunch;
            }

            public MMF_HSScreenKick Screen(float pAberration, float pRadial, float pBloom, float pExposure, float pLens, float pSaturation, float pDuration)
            {
                MMF_HSScreenKick lKick = Add<MMF_HSScreenKick>("Screen kick");
                lKick.Aberration = pAberration;
                lKick.RadialBlur = pRadial;
                lKick.Bloom = pBloom;
                lKick.Exposure = pExposure;
                lKick.Lens = pLens;
                lKick.Saturation = pSaturation;
                lKick.Duration = pDuration;
                lKick.Fade = new AnimationCurve(new Keyframe(0f, 1f, 0f, -2.5f), new Keyframe(1f, 0f, 0f, 0f));
                return lKick;
            }

            public MMF_HSFlash Flash(Color pColor, float pOpacity)
            {
                MMF_HSFlash lFlash = Add<MMF_HSFlash>("Flash");
                lFlash.FlashColor = pColor;
                lFlash.Opacity = pOpacity;
                return lFlash;
            }

            public MMF_HSVignette Vignette(Color pColor, float pAmount)
            {
                MMF_HSVignette lVignette = Add<MMF_HSVignette>("Vignette");
                lVignette.VignetteColor = pColor;
                lVignette.Amount01 = pAmount;
                return lVignette;
            }

            public MMF_HSImpactFrame Impact(ImpactReason pReason, bool pInWorld, int pFrames, float pStrength)
            {
                MMF_HSImpactFrame lImpact = Add<MMF_HSImpactFrame>("Impact frame");
                lImpact.Reason = pReason;
                lImpact.InWorld = pInWorld;
                lImpact.InkFrames = pFrames;
                lImpact.Strength01 = pStrength;
                return lImpact;
            }

            public MMF_HSSpeedLines Lines(float pBurst)
            {
                MMF_HSSpeedLines lLines = Add<MMF_HSSpeedLines>("Speed lines");
                lLines.Burst = pBurst;
                return lLines;
            }

            public MMF_HSHitstop Hitstop(float pDuration)
            {
                MMF_HSHitstop lStop = Add<MMF_HSHitstop>("Hit stop");
                lStop.Duration = pDuration;
                lStop.TimeScale = 0f;
                return lStop;
            }

            // Starts after pDelay (unscaled) : a slow motion right after a hit stop.
            public MMF_HSSlowMo SlowMo(float pScale, float pDuration, float pDelay)
            {
                MMF_HSSlowMo lSlow = Add<MMF_HSSlowMo>("Slow motion");
                lSlow.TimeScale = pScale;
                lSlow.Duration = pDuration;
                lSlow.Timing.InitialDelay = pDelay;
                lSlow.Timing.TimescaleMode = TimescaleModes.Unscaled;
                return lSlow;
            }

            public MMF_HSRumble Rumble(float pPower)
            {
                MMF_HSRumble lRumble = Add<MMF_HSRumble>("Pad rumble");
                lRumble.Power = pPower;
                return lRumble;
            }

            public MMF_HSDuck Duck(float pDuration)
            {
                MMF_HSDuck lDuck = Add<MMF_HSDuck>("Music duck");
                lDuck.Duration = pDuration;
                return lDuck;
            }

            public MMF_HSBody Body(float pUp, float pForward, float pFlash)
            {
                MMF_HSBody lBody = Add<MMF_HSBody>("Runner squash/stretch");
                lBody.Up = pUp;
                lBody.Forward = pForward;
                lBody.Flash = pFlash;
                return lBody;
            }

            public MMF_HSPopup Popup(string pText, Color pColor)
            {
                MMF_HSPopup lPopup = Add<MMF_HSPopup>("Popup");
                lPopup.Text = pText;
                lPopup.TextColor = pColor;
                return lPopup;
            }

            public MMF_HSFX FX(HSFXKind pKind, HSColorSource pSource, Color pColor, int pCount, float pSize, float pSpeed, float pLife, HSDirection pDirection)
            {
                MMF_HSFX lFX = Add<MMF_HSFX>("FX " + pKind);
                lFX.Kind = pKind;
                lFX.ColorSource = pSource;
                lFX.EffectColor = pColor;
                lFX.Count = pCount;
                lFX.Size = pSize;
                lFX.Speed = pSpeed;
                lFX.Life = pLife;
                lFX.Direction = pDirection;
                return lFX;
            }
        }
    }
}
