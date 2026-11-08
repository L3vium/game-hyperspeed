using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum DecorKind { Islands, City, Desert, Industrial, Ice, Space, Void, Night, Prehistoric, Forest, Canyon, Castle, Neon, Volcano }

    // Palette + decor style of a level.
    public class Theme
    {
        public string name;
        public DecorKind decor;
        public Color skyTop, skyHorizon, skyBottom, cloud, sun;
        public Color fog;
        public float fogDensity = 0.0022f;
        public Color light = Color.white;
        public float lightIntensity = 1.25f;
        public Vector3 sunDirection = new Vector3(0.35f, 0.4f, 0.85f);
        public Color ambient;
        public Color roadA, roadB, roadEdge, roadSide;
        public Color road;
        public Color groundC;
        public Color wall;
        public Color accent;
        public Color decorA, decorB, decorC;
        public Color shade = new Color(0.62f, 0.55f, 0.8f);
        public float stars;
        public float rays;
        public float clouds = 0.55f;

        private static Color Hex(string pHex)
        {
            Color lColor;
            ColorUtility.TryParseHtmlString("#" + pHex, out lColor);
            return lColor;
        }

        public static Theme Islands() => new Theme
        {
            road = Hex("5E5A6B"), groundC = Hex("B7E04F"),
            name = "SUNNY ISLES", decor = DecorKind.Islands,
            skyTop = Hex("1E6BFF"), skyHorizon = Hex("9ED8FF"), skyBottom = Hex("E8F6FF"), cloud = Color.white, sun = new Color(2f, 1.8f, 1.4f),
            fog = Hex("BFE6FF"), fogDensity = 0.0016f, ambient = Hex("A6C8FF"),
            roadA = Hex("8FD43A"), roadB = Hex("7CC22E"), roadEdge = Hex("FFE35A"), roadSide = Hex("E89B3C"),
            wall = Hex("F2B04C"), accent = Hex("FF6A00"),
            decorA = Hex("FF7A1E"), decorB = Hex("F7D154"), decorC = Hex("E0523A"), shade = Hex("C9A6D9"), clouds = 0.7f
        };

        public static Theme City() => new Theme
        {
            road = Hex("24285E"), groundC = Hex("7E86F2"),
            name = "NEON AVENUE", decor = DecorKind.City,
            skyTop = Hex("5A7BFF"), skyHorizon = Hex("D9E6FF"), skyBottom = Hex("F2F0FF"), cloud = Hex("FFFFFF"), sun = new Color(1.8f, 1.7f, 1.6f),
            fog = Hex("D6DBFF"), fogDensity = 0.0018f, ambient = Hex("B7B8FF"),
            roadA = Hex("5F68E6"), roadB = Hex("4C55D2"), roadEdge = Hex("FF3B6B"), roadSide = Hex("2B2F78"),
            wall = Hex("C8CCF0"), accent = Hex("FF2BD6"),
            decorA = Hex("FF4FA3"), decorB = Hex("2FD8FF"), decorC = Hex("FFC23D"), shade = Hex("9A8FE0")
        };

        public static Theme Desert() => new Theme
        {
            road = Hex("B83A7A"), groundC = Hex("FF9ACD"),
            name = "PINK DUNES", decor = DecorKind.Desert,
            skyTop = Hex("5FA8FF"), skyHorizon = Hex("FFC3E8"), skyBottom = Hex("FF9FD0"), cloud = Hex("FFFFFF"), sun = new Color(2f, 1.6f, 1.7f),
            fog = Hex("FFB3DD"), fogDensity = 0.002f, ambient = Hex("FFB0D8"),
            roadA = Hex("FF6FB5"), roadB = Hex("F75FA8"), roadEdge = Hex("FFFFFF"), roadSide = Hex("B83A7A"),
            wall = Hex("FF8CC6"), accent = Hex("FF2E63"),
            decorA = Hex("2F6B4E"), decorB = Hex("3D2A3A"), decorC = Hex("FF9ACD"), shade = Hex("C46FB0"), rays = 0.6f
        };

        public static Theme Industrial() => new Theme
        {
            road = Hex("4A1B12"), groundC = Hex("F28A4C"),
            name = "FURNACE LINE", decor = DecorKind.Industrial,
            skyTop = Hex("FF3D1F"), skyHorizon = Hex("FF9A3D"), skyBottom = Hex("FFC36B"), cloud = Hex("FFD9A0"), sun = new Color(2.2f, 1.3f, 0.6f),
            fog = Hex("FF8A4A"), fogDensity = 0.0026f, ambient = Hex("FF9E6E"), light = Hex("FFD2A6"),
            roadA = Hex("E8693C"), roadB = Hex("D95B33"), roadEdge = Hex("FFE066"), roadSide = Hex("7A2414"),
            wall = Hex("C9431F"), accent = Hex("FFD000"),
            decorA = Hex("B33A1A"), decorB = Hex("FF7A2E"), decorC = Hex("5C1A10"), shade = Hex("B0405A"), clouds = 0.4f
        };

        public static Theme Ice() => new Theme
        {
            road = Hex("7C6CE0"), groundC = Hex("FFFFFF"),
            name = "CRYSTAL PEAKS", decor = DecorKind.Ice,
            skyTop = Hex("3B2A8F"), skyHorizon = Hex("B8A8FF"), skyBottom = Hex("E9E6FF"), cloud = Hex("F2EEFF"), sun = new Color(1.6f, 1.5f, 2f),
            fog = Hex("CFC8FF"), fogDensity = 0.0022f, ambient = Hex("B8B0FF"),
            roadA = Hex("EEF2FF"), roadB = Hex("DCE3FA"), roadEdge = Hex("FF2E9A"), roadSide = Hex("7C6CE0"),
            wall = Hex("A99CFF"), accent = Hex("FF2E9A"),
            decorA = Hex("8FA8FF"), decorB = Hex("E8E4FF"), decorC = Hex("5A3FD1"), shade = Hex("8E86D8"), stars = 0.3f
        };

        public static Theme Space() => new Theme
        {
            road = Hex("1A1F44"), groundC = Hex("4A4F85"),
            name = "HYPER TUNNEL", decor = DecorKind.Space,
            skyTop = Hex("050A3A"), skyHorizon = Hex("1D4BFF"), skyBottom = Hex("0A1460"), cloud = Hex("3FD0FF"), sun = new Color(1.2f, 1.6f, 2.2f),
            fog = Hex("0E2A8A"), fogDensity = 0.0018f, ambient = Hex("4A6BFF"), light = Hex("BFD6FF"),
            roadA = Hex("3A3F6E"), roadB = Hex("30355E"), roadEdge = Hex("2BE7FF"), roadSide = Hex("FFC400"),
            wall = Hex("2B2F55"), accent = Hex("2BE7FF"),
            decorA = Hex("1A1F44"), decorB = Hex("2BE7FF"), decorC = Hex("FF4FD8"), shade = Hex("5A5FB0"), stars = 1f, clouds = 0.35f
        };

        public static Theme Void() => new Theme
        {
            road = Hex("C9A8FF"), groundC = Hex("FFE3F0"),
            name = "WHITE LIGHT", decor = DecorKind.Void,
            skyTop = Hex("FFB6D9"), skyHorizon = Hex("FFF2E8"), skyBottom = Hex("FFFFFF"), cloud = Hex("FFD6EC"), sun = new Color(2.5f, 2.3f, 2.2f),
            fog = Hex("FFE9F3"), fogDensity = 0.0024f, ambient = Hex("FFE0F0"),
            roadA = Hex("FFFFFF"), roadB = Hex("F1F1FA"), roadEdge = Hex("FF3D8B"), roadSide = Hex("C9A8FF"),
            wall = Hex("F7E6FF"), accent = Hex("FF3D8B"),
            decorA = Hex("FFC2DD"), decorB = Hex("FFFFFF"), decorC = Hex("C9A8FF"), shade = Hex("D8B8F0"), rays = 1f
        };

        public static Theme Forest() => new Theme
        {
            road = Hex("4C7A1E"), groundC = Hex("C4E23C"),
            name = "THE CANOPY", decor = DecorKind.Forest,
            skyTop = Hex("0F7A6B"), skyHorizon = Hex("BFEFA0"), skyBottom = Hex("EAF7C8"), cloud = Hex("F2FFE0"), sun = new Color(2.2f, 2f, 1.3f),
            fog = Hex("B6E6A0"), fogDensity = 0.0018f, ambient = Hex("B0DFA0"),
            roadA = Hex("9BD12E"), roadB = Hex("7FBC24"), roadEdge = Hex("31D6C3"), roadSide = Hex("6B4426"),
            wall = Hex("B5823F"), accent = Hex("C23BFF"),
            decorA = Hex("2F8F3A"), decorB = Hex("E2D13C"), decorC = Hex("5A3A22"), shade = Hex("7FA08C"), clouds = 0.5f
        };

        public static Theme GoldDunes() => new Theme
        {
            road = Hex("C48A2C"), groundC = Hex("FFD06A"),
            name = "AMBER SEA", decor = DecorKind.Desert,
            skyTop = Hex("3A2A78"), skyHorizon = Hex("E47A5C"), skyBottom = Hex("FFC67A"), cloud = Hex("FFE1B0"), sun = new Color(2.4f, 1.5f, 0.9f),
            fog = Hex("E89A6A"), fogDensity = 0.0018f, ambient = Hex("E0A890"),
            roadA = Hex("F5B84A"), roadB = Hex("E8A63A"), roadEdge = Hex("7A2BFF"), roadSide = Hex("8A4A20"),
            wall = Hex("D98A3A"), accent = Hex("7A2BFF"),
            decorA = Hex("2F7A52"), decorB = Hex("2A1F4A"), decorC = Hex("8E4BFF"), shade = Hex("9060A0"), rays = 0.5f
        };

        public static Theme LaserCanyon() => new Theme
        {
            road = Hex("3A3438"), groundC = Hex("6E6468"),
            name = "RED LINE", decor = DecorKind.Canyon,
            skyTop = Hex("3A3238"), skyHorizon = Hex("8A7A78"), skyBottom = Hex("A89890"), cloud = Hex("B0A098"), sun = new Color(1.8f, 1.4f, 1.3f),
            fog = Hex("8E7E7C"), fogDensity = 0.0022f, ambient = Hex("9A8A8A"), light = Hex("FFD8C8"),
            roadA = Hex("6A6064"), roadB = Hex("5C5358"), roadEdge = Hex("FF2A3C"), roadSide = Hex("2A2428"),
            wall = Hex("4A4246"), accent = Hex("FF2A3C"),
            decorA = Hex("2E282E"), decorB = Hex("4A4048"), decorC = Hex("6A5A62"), shade = Hex("6A5070"), clouds = 0.25f
        };

        public static Theme SkyCastle() => new Theme
        {
            road = Hex("B98A5E"), groundC = Hex("E4C49A"),
            name = "HIGH KEEP", decor = DecorKind.Castle,
            skyTop = Hex("2E7BE8"), skyHorizon = Hex("BEEAFF"), skyBottom = Hex("FFFFFF"), cloud = Hex("FFFFFF"), sun = new Color(2.3f, 2.2f, 2f),
            fog = Hex("E0F2FF"), fogDensity = 0.0014f, ambient = Hex("C8DCFF"),
            roadA = Hex("D9AE7C"), roadB = Hex("C99A68"), roadEdge = Hex("3F6BFF"), roadSide = Hex("8FA6C8"),
            wall = Hex("A8BEDC"), accent = Hex("3F6BFF"),
            decorA = Hex("8FA6C8"), decorB = Hex("DCE8F8"), decorC = Hex("4A5FA0"), shade = Hex("9CA8D8"), clouds = 0.8f
        };

        public static Theme NeonNight() => new Theme
        {
            road = Hex("1C1440"), groundC = Hex("5A2FB0"),
            name = "AFTER HOURS", decor = DecorKind.Neon,
            skyTop = Hex("120A3A"), skyHorizon = Hex("C42BD8"), skyBottom = Hex("FF5AA8"), cloud = Hex("E058C8"), sun = new Color(2.4f, 1f, 2f),
            fog = Hex("6A1FA0"), fogDensity = 0.0022f, ambient = Hex("9060E0"), light = Hex("E0B8FF"),
            roadA = Hex("3A2478"), roadB = Hex("2E1C64"), roadEdge = Hex("2BE7FF"), roadSide = Hex("FF2BD6"),
            wall = Hex("4A2FA0"), accent = Hex("2BE7FF"),
            decorA = Hex("FF2BD6"), decorB = Hex("2BE7FF"), decorC = Hex("2A1A60"), shade = Hex("5A3AA8"), stars = 0.6f, clouds = 0.4f
        };

        public static Theme Prehistoric() => new Theme
        {
            road = Hex("5B4632"), groundC = Hex("9CCB3B"),
            name = "66 MILLION BC", decor = DecorKind.Prehistoric,
            skyTop = Hex("E8622C"), skyHorizon = Hex("FFC36B"), skyBottom = Hex("FFE2A8"), cloud = Hex("FFE9C9"), sun = new Color(2.4f, 1.5f, 0.8f),
            fog = Hex("FFB870"), fogDensity = 0.0018f, ambient = Hex("FFC98E"), light = Hex("FFE0B0"),
            roadA = Hex("6FAA2C"), roadB = Hex("588F22"), roadEdge = Hex("FFE066"), roadSide = Hex("6B3E22"),
            wall = Hex("8A5A32"), accent = Hex("FF3D1F"),
            decorA = Hex("7A4A28"), decorB = Hex("3F6E23"), decorC = Hex("C25B22"), shade = Hex("B0605A"), rays = 0.6f
        };

        // Black basalt, rivers of lava, ash in the sky and cones spitting fire on the horizon.
        public static Theme Volcano() => new Theme
        {
            road = Hex("2A1A1A"), groundC = Hex("FF5A1F"),
            name = "MAGMA RUN", decor = DecorKind.Volcano,
            skyTop = Hex("2A0A0A"), skyHorizon = Hex("FF5A1F"), skyBottom = Hex("FFB347"), cloud = Hex("6A4848"), sun = new Color(2.6f, 1.2f, 0.4f),
            fog = Hex("8A2A14"), fogDensity = 0.0024f, ambient = Hex("C0503A"), light = Hex("FFC09A"),
            roadA = Hex("3A2A2E"), roadB = Hex("2E2226"), roadEdge = Hex("FF7A1F"), roadSide = Hex("1A1012"),
            wall = Hex("4A2E2A"), accent = Hex("FF5A00"),
            decorA = Hex("2A1A1C"), decorB = Hex("FF7A1F"), decorC = Hex("5A2A1A"), shade = Hex("8A3A4A"), clouds = 0.3f, rays = 0.4f
        };

        public static Theme Night() => new Theme
        {
            road = Hex("120E16"), groundC = Hex("3A3344"),
            name = "GRAVEYARD SHIFT", decor = DecorKind.Night,
            skyTop = Hex("0B0410"), skyHorizon = Hex("5A0A1E"), skyBottom = Hex("1A0710"), cloud = Hex("8A1030"), sun = new Color(2.5f, 0.4f, 0.4f),
            fog = Hex("3A0816"), fogDensity = 0.0028f, ambient = Hex("6A2A44"), light = Hex("FF8A9A"), lightIntensity = 1.1f,
            roadA = Hex("2E2A36"), roadB = Hex("27232E"), roadEdge = Hex("FF1744"), roadSide = Hex("120E16"),
            wall = Hex("3A3344"), accent = Hex("FF1744"),
            decorA = Hex("1E1A24"), decorB = Hex("FF1744"), decorC = Hex("4A4256"), shade = Hex("5A3A6A"), stars = 0.8f, clouds = 0.5f
        };
    }
}
