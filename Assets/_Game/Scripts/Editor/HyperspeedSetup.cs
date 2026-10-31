using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // One-click project configuration and Windows build.
    public static class HyperspeedSetup
    {
        private const string SCENES_DIR = "Assets/_Game/Scenes/";
        private const string TITLE_SCENE = SCENES_DIR + "title_screen.unity";
        private const string CORE_SCENE = SCENES_DIR + "core.unity";
        private const string STYLIZE_MATERIAL = "Assets/_Game/Settings/M_Stylize.mat";
        private const string GAME_ASSETS = "Assets/_Game/Resources/GameAssets.asset";
        private const string FONT_DIR = "Assets/_Game/Art/Fonts/";
        private const string ICON = "Assets/_Game/Art/Icon.png";
        private const string IMPACT_PREFAB = "Assets/_Plugins/Vefects/Easy Impact Frames/VFX/Impact Frames/Particles/VFX_Impact_Frame_01.prefab";
        private const string TITLE_FONT_SOURCE = "Assets/_Plugins/Feel/FeelDemos/UIToolkitFeedbacksDemo/Fonts/UIToolkitDemo_BigJohn.otf";
        private const string BODY_FONT = "Assets/_Plugins/Feel/MMTools/Demos/MMTween/Fonts/Lato/SDF/Lato-Black SDF.asset";
        public const string BUILD_PATH = "Builds/Windows/HyperSpeed.exe";

        [MenuItem("Hyperspeed/Setup Project")]
        public static void SetupAll()
        {
            ConfigurePlayer();
            CreateGameAssets();
            ConfigureRenderer();
            ConfigureAudio();
            BuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Hyperspeed] Project setup done.");
        }

        [MenuItem("Hyperspeed/Configure Player Settings")]
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Isart Digital";
            PlayerSettings.productName = "Hyper Speed";
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            TextureImporter lImporter = AssetImporter.GetAtPath(ICON) as TextureImporter;
            if (lImporter != null)
            {
                lImporter.textureType = TextureImporterType.Default;
                lImporter.mipmapEnabled = false;
                lImporter.textureCompression = TextureImporterCompression.Uncompressed;
                lImporter.npotScale = TextureImporterNPOTScale.None;
                lImporter.alphaIsTransparency = true;
                lImporter.SaveAndReimport();
                Texture2D lIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON);
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { lIcon }, IconKind.Any);
                int lCount = PlayerSettings.GetIcons(NamedBuildTarget.Standalone, IconKind.Any).Length;
                Texture2D[] lIcons = new Texture2D[lCount];
                for (int i = 0; i < lCount; i++) lIcons[i] = lIcon;
                PlayerSettings.SetIcons(NamedBuildTarget.Standalone, lIcons, IconKind.Any);
            }
        }

        [MenuItem("Hyperspeed/Create Game Assets")]
        public static void CreateGameAssets()
        {
            Directory.CreateDirectory(FONT_DIR);
            Directory.CreateDirectory(Path.GetDirectoryName(GAME_ASSETS));

            GameAssets lAssets = AssetDatabase.LoadAssetAtPath<GameAssets>(GAME_ASSETS);
            if (lAssets == null)
            {
                lAssets = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(lAssets, GAME_ASSETS);
            }

            string lTitlePath = FONT_DIR + "BigJohn SDF.asset";
            TMP_FontAsset lTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(lTitlePath);
            Font lSource = AssetDatabase.LoadAssetAtPath<Font>(TITLE_FONT_SOURCE);
            if (lTitle == null && lSource != null)
            {
                lTitle = TMP_FontAsset.CreateFontAsset(lSource, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                lTitle.name = "BigJohn SDF";
                lTitle.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
                AssetDatabase.CreateAsset(lTitle, lTitlePath);
                lTitle.material.name = "BigJohn SDF Material";
                AssetDatabase.AddObjectToAsset(lTitle.material, lTitle);
                foreach (Texture2D lAtlas in lTitle.atlasTextures)
                {
                    lAtlas.name = "BigJohn SDF Atlas";
                    AssetDatabase.AddObjectToAsset(lAtlas, lTitle);
                }
                TMP_FontAsset lFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BODY_FONT);
                if (lFallback != null) lTitle.fallbackFontAssetTable = new List<TMP_FontAsset> { lFallback };
                EditorUtility.SetDirty(lTitle);
            }
            lAssets.titleFont = lTitle;
            lAssets.bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BODY_FONT);
            lAssets.impactFramePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IMPACT_PREFAB);
            EditorUtility.SetDirty(lAssets);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Hyperspeed/Configure Renderer")]
        public static void ConfigureRenderer()
        {
            Material lMaterial = AssetDatabase.LoadAssetAtPath<Material>(STYLIZE_MATERIAL);
            if (lMaterial == null)
            {
                lMaterial = new Material(Shader.Find("Hyperspeed/Stylize"));
                AssetDatabase.CreateAsset(lMaterial, STYLIZE_MATERIAL);
            }

            foreach (string lGuid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/_Game/Settings" }))
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuid);
                UniversalRendererData lData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(lPath);
                bool lHas = false;
                foreach (ScriptableRendererFeature lFeature in lData.rendererFeatures)
                    if (lFeature is FullScreenPassRendererFeature && lFeature.name == "HS Stylize") lHas = true;
                if (lHas) continue;

                FullScreenPassRendererFeature lStylize = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                lStylize.name = "HS Stylize";
                lStylize.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
                lStylize.fetchColorBuffer = true;
                lStylize.requirements = ScriptableRenderPassInput.Color;
                lStylize.passMaterial = lMaterial;
                lStylize.passIndex = 0;
                AssetDatabase.AddObjectToAsset(lStylize, lData);
                lData.rendererFeatures.Add(lStylize);
                lData.SetDirty();
                EditorUtility.SetDirty(lData);
            }

            foreach (string lGuid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/_Game/Settings" }))
            {
                UniversalRenderPipelineAsset lAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(lGuid));
                lAsset.shadowDistance = 160f;
                lAsset.supportsHDR = true;
                EditorUtility.SetDirty(lAsset);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Hyperspeed/Configure Audio")]
        public static void ConfigureAudio()
        {
            foreach (string lGuid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game/Resources/Audio" }))
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuid);
                AudioImporter lImporter = (AudioImporter)AssetImporter.GetAtPath(lPath);
                bool lMusic = Path.GetFileName(lPath).StartsWith("music_");
                AudioImporterSampleSettings lSettings = lImporter.defaultSampleSettings;
                lSettings.loadType = lMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                lSettings.compressionFormat = lMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                lSettings.quality = 0.7f;
                lImporter.defaultSampleSettings = lSettings;
                lImporter.forceToMono = !lMusic;
                lImporter.SaveAndReimport();
            }
        }

        [MenuItem("Hyperspeed/Build Scenes")]
        public static void BuildScenes()
        {
            // Title scene
            Scene lTitle = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject lManagers = new GameObject("--- MANAGERS ---");
            GameObject lTitleObject = new GameObject("TitleScreen");
            lTitleObject.transform.SetParent(lManagers.transform);
            lTitleObject.AddComponent<TitleScreen>();
            new GameObject("--- STATIC ---");
            EditorSceneManager.SaveScene(lTitle, TITLE_SCENE);

            // Core scene : keep the header organization, the level is generated at runtime.
            Scene lCore = EditorSceneManager.OpenScene(CORE_SCENE, OpenSceneMode.Single);
            string[] lObsolete = { "Main Camera", "Directional Light", "Global Volume", "Canvas", "EventSystem" };
            foreach (GameObject lRoot in lCore.GetRootGameObjects())
            {
                foreach (string lName in lObsolete)
                {
                    Transform lFound = lRoot.name == lName ? lRoot.transform : FindDeep(lRoot.transform, lName);
                    if (lFound != null) Object.DestroyImmediate(lFound.gameObject);
                }
            }
            GameObject lHeader = null;
            foreach (GameObject lRoot in lCore.GetRootGameObjects())
                if (lRoot.name == "--- GAME_MANAGER ---") lHeader = lRoot;
            if (lHeader == null) lHeader = new GameObject("--- GAME_MANAGER ---");
            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                GameObject lManager = new GameObject("GameManager");
                lManager.transform.SetParent(lHeader.transform);
                lManager.AddComponent<GameManager>();
            }
            EditorSceneManager.SaveScene(lCore, CORE_SCENE);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TITLE_SCENE, true),
                new EditorBuildSettingsScene(CORE_SCENE, true)
            };
            EditorSceneManager.OpenScene(TITLE_SCENE, OpenSceneMode.Single);
        }

        private static Transform FindDeep(Transform pParent, string pName)
        {
            foreach (Transform lChild in pParent)
            {
                if (lChild.name == pName) return lChild;
                Transform lFound = FindDeep(lChild, pName);
                if (lFound != null) return lFound;
            }
            return null;
        }

        [MenuItem("Hyperspeed/Build Windows")]
        public static string BuildWindows()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BUILD_PATH));
            BuildPlayerOptions lOptions = new BuildPlayerOptions
            {
                scenes = new[] { TITLE_SCENE, CORE_SCENE },
                locationPathName = BUILD_PATH,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport lReport = BuildPipeline.BuildPlayer(lOptions);
            string lSummary = lReport.summary.result + " | " + lReport.summary.totalSize / (1024 * 1024) + " MB | errors " + lReport.summary.totalErrors + " | " + lReport.summary.totalTime;
            Debug.Log("[Hyperspeed] Build : " + lSummary);
            return lSummary;
        }
    }
}
