using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // A stage made editable : baked from its recipe once, then shaped by hand in the Level Design window
    // (Hyperspeed > Level Design). Lives in Resources/Stages/stage_N. When it exists and is enabled, the game builds the
    // stage from it instead of from the recipe in LevelDatabase.
    [CreateAssetMenu(menuName = "Hyperspeed/Stage Layout", fileName = "stage_0")]
    public class StageAsset : ScriptableObject
    {
        public const string FOLDER = "Stages";

        [Tooltip("Off : the game ignores this asset and builds the stage from its recipe.")]
        public bool useInGame = true;
        public StageData data = new StageData();

        public static string ResourcePath(int pIndex) => FOLDER + "/stage_" + pIndex;

        public static StageAsset Load(int pIndex)
        {
            StageAsset lAsset = Resources.Load<StageAsset>(ResourcePath(pIndex));
            return lAsset != null && lAsset.useInGame && lAsset.data != null && lAsset.data.segments.Count > 0 ? lAsset : null;
        }
    }
}
