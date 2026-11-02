using TMPro;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Asset references that live outside Resources (fonts, plugin prefabs). Filled by the editor setup.
    [CreateAssetMenu(menuName = "Hyperspeed/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;
        public TMP_FontAsset thinFont;
        public GameObject impactFramePrefab;
        public GameObject[] ambientParticles;
        public GameObject sparkBurstPrefab;
        public Texture2D[] icons;

        private static GameAssets _instance;

        public static GameAssets Instance
        {
            get
            {
                if (_instance == null) _instance = Resources.Load<GameAssets>("GameAssets");
                if (_instance == null) _instance = CreateInstance<GameAssets>();
                return _instance;
            }
        }

        public static TMP_FontAsset TitleFont => Instance.titleFont != null ? Instance.titleFont : BodyFont;

        public static TMP_FontAsset ThinFont => Instance.thinFont != null ? Instance.thinFont : BodyFont;

        public static TMP_FontAsset BodyFont => Instance.bodyFont != null ? Instance.bodyFont : TMP_Settings.defaultFontAsset;
    }
}
