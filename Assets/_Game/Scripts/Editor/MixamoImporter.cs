using System.IO;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Import rules for Assets/_Game/Art/Character/Mixamo : the character becomes a Humanoid with its own avatar,
    // every clip reuses that avatar, loops are flagged by name and root motion is baked into the pose (in place).
    public class MixamoImporter : AssetPostprocessor
    {
        public const string FOLDER = "Assets/_Game/Art/Character/Mixamo/";
        public const string CHARACTER = FOLDER + "runner_character.fbx";

        private static readonly string[] LOOPS =
        {
            "idle", "idle_ready", "look_around", "jog", "run", "run_fast", "sprint", "fall", "glide", "dive",
            "slide", "wall_run", "wall_climb", "wall_slide", "grind", "hang", "rider_pose"
        };

        private static bool _wirePending;

        private bool InFolder => assetPath.Replace('\\', '/').StartsWith(FOLDER) && assetPath.ToLower().EndsWith(".fbx");

        private static bool IsCharacter(string pPath)
        {
            string lName = Path.GetFileNameWithoutExtension(pPath).ToLower();
            return lName == "runner_character" || lName == "rider_character";
        }

        private void OnPreprocessModel()
        {
            if (!InFolder) return;
            ModelImporter lImporter = (ModelImporter)assetImporter;
            lImporter.animationType = ModelImporterAnimationType.Human;
            lImporter.globalScale = 1f;
            lImporter.useFileScale = true;
            if (IsCharacter(assetPath))
            {
                lImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                lImporter.importAnimation = false;
                lImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                lImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
                return;
            }
            lImporter.importAnimation = true;
            lImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            Avatar lAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(CHARACTER);
            if (lAvatar == null)
                foreach (Object lObject in AssetDatabase.LoadAllAssetsAtPath(CHARACTER))
                    if (lObject is Avatar) lAvatar = (Avatar)lObject;
            if (lAvatar != null)
            {
                lImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                lImporter.sourceAvatar = lAvatar;
            }
            else lImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }

        private void OnPreprocessAnimation()
        {
            if (!InFolder || IsCharacter(assetPath)) return;
            ModelImporter lImporter = (ModelImporter)assetImporter;
            string lName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
            bool lLoop = System.Array.IndexOf(LOOPS, lName) >= 0;
            ModelImporterClipAnimation[] lClips = lImporter.defaultClipAnimations;
            for (int i = 0; i < lClips.Length; i++)
            {
                lClips[i].name = lName;
                lClips[i].loopTime = lLoop;
                lClips[i].loopPose = lLoop;
                lClips[i].lockRootRotation = true;
                lClips[i].lockRootHeightY = true;
                lClips[i].lockRootPositionXZ = true;
                lClips[i].keepOriginalOrientation = true;
                lClips[i].keepOriginalPositionY = true;
                lClips[i].keepOriginalPositionXZ = true;
            }
            lImporter.clipAnimations = lClips;
        }

        // The character arrived after its clips : reimport the clips so they pick up its avatar.
        // Any import in the folder rewires the runner (model + generated controller) once the import settles.
        private static void OnPostprocessAllAssets(string[] pImported, string[] pDeleted, string[] pMoved, string[] pMovedFrom)
        {
            bool lTouched = false;
            foreach (string lPath in pImported) if (lPath.Replace('\\', '/').StartsWith(FOLDER) && lPath.ToLower().EndsWith(".fbx")) lTouched = true;
            if (lTouched && !_wirePending)
            {
                _wirePending = true;
                EditorApplication.delayCall += () =>
                {
                    _wirePending = false;
                    HyperspeedSetup.WireRunner();
                };
            }
            foreach (string lPath in pImported)
            {
                if (!lPath.Replace('\\', '/').StartsWith(FOLDER) || !IsCharacter(lPath)) continue;
                foreach (string lGuid in AssetDatabase.FindAssets("t:Model", new[] { FOLDER.TrimEnd('/') }))
                {
                    string lClip = AssetDatabase.GUIDToAssetPath(lGuid);
                    if (IsCharacter(lClip)) continue;
                    ModelImporter lImporter = AssetImporter.GetAtPath(lClip) as ModelImporter;
                    if (lImporter != null && lImporter.sourceAvatar == null) AssetDatabase.ImportAsset(lClip, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
