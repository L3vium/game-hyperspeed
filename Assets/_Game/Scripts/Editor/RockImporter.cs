using UnityEditor;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Scanned rocks (Art/Environment/Rocks) : readable meshes merged at runtime by RockKit, no imported materials.
    public class RockImporter : AssetPostprocessor
    {
        private const string FOLDER = "Assets/_Game/Art/Environment/Rocks/";

        private void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(FOLDER)) return;
            ModelImporter lImporter = (ModelImporter)assetImporter;
            lImporter.isReadable = true;
            lImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            lImporter.animationType = ModelImporterAnimationType.None;
            lImporter.importAnimation = false;
            lImporter.importCameras = false;
            lImporter.importLights = false;
            lImporter.meshCompression = ModelImporterMeshCompression.Off;
            lImporter.importNormals = ModelImporterNormals.Import;
            lImporter.addCollider = false;
        }

        // Rock and ground albedos : seen at grazing angles at 200 m/s, so anisotropic filtering and full resolution.
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/_Game/Art/Environment/")) return;
            TextureImporter lImporter = (TextureImporter)assetImporter;
            lImporter.anisoLevel = 8;
            lImporter.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            lImporter.maxTextureSize = 2048;
            lImporter.mipmapEnabled = true;
        }
    }
}
