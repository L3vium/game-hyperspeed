using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum DecorKind { Islands, City, Desert, Industrial, Ice, Space, Void, Night, Prehistoric }

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
            decorA = Hex("FF8A1E"), decorB = Hex("F3C877"), decorC = Hex("C9874A"), shade = Hex("C9A6D9"), clouds = 0.7f
        };

        public static Theme City() => new Theme
        {
            road = Hex("3F4470"), groundC = Hex("A2A7DA"),
            name = "NEON AVENUE", decor = DecorKind.City,
            skyTop = Hex("5A7BFF"), skyHorizon = Hex("D9E6FF"), skyBottom = Hex("F2F0FF"), cloud = Hex("FFFFFF"), sun = new Color(1.8f, 1.7f, 1.6f),
            fog = Hex("D6DBFF"), fogDensity = 0.0018f, ambient = Hex("B7B8FF"),
            roadA = Hex("8E92C9"), roadB = Hex("8086BD"), roadEdge = Hex("FF3B6B"), roadSide = Hex("3A3F78"),
            wall = Hex("C8CCF0"), accent = Hex("FF2BD6"),
            decorA = Hex("C9CDF2"), decorB = Hex("4BE3FF"), decorC = Hex("6A5CFF"), shade = Hex("9A8FE0")
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
