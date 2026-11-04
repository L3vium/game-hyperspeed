using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Import rules for Art/Vehicles and Art/Creatures : vertex colours only, no imported materials,
    // creatures keep their one looping clip through a legacy Animation component that plays by itself.
    public class RivalModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            string lPath = assetPath.Replace('\\', '/');
            bool lVehicle = lPath.StartsWith("Assets/_Game/Art/Vehicles/");
            bool lCreature = lPath.StartsWith("Assets/_Game/Art/Creatures/");
            if (!lVehicle && !lCreature) return;
            ModelImporter lImporter = (ModelImporter)assetImporter;
            lImporter.globalScale = 1f;
            lImporter.useFileScale = true;
            lImporter.importCameras = false;
            lImporter.importLights = false;
            lImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            lImporter.importNormals = ModelImporterNormals.Import;
            if (lCreature)
            {
                lImporter.animationType = ModelImporterAnimationType.Legacy;
                lImporter.importAnimation = true;
                lImporter.animationWrapMode = WrapMode.Loop;
            }
            else
            {
                lImporter.animationType = ModelImporterAnimationType.None;
                lImporter.importAnimation = false;
            }
        }
    }
}
