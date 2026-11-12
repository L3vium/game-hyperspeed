using System.IO;
using UnityEditor;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // Import rules for every clip of the game, applied automatically on import (2026-11-12 : clips added later kept
    // Unity's defaults, the music stems decoded on the main thread at GO, the effects were folded to mono).
    //   music stems (music_wN_*) : compressed in memory, decoded on the audio thread, loaded in the background ;
    //   other music              : streamed from disk ;
    //   effects and UI           : decompressed on load, preloaded, kept in stereo.
    public class AudioImportRules : AssetPostprocessor
    {
        private const string FOLDER = "Assets/_Game/Resources/Audio";

        private void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(FOLDER)) return;
            Apply((AudioImporter)assetImporter, Path.GetFileNameWithoutExtension(assetPath));
        }

        public static void Apply(AudioImporter pImporter, string pName)
        {
            bool lMusic = pName.StartsWith("music_");
            bool lStem = lMusic && pName.StartsWith("music_w");
            AudioImporterSampleSettings lSettings = pImporter.defaultSampleSettings;
            if (lStem)
            {
                lSettings.loadType = AudioClipLoadType.CompressedInMemory;
                lSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                lSettings.quality = 0.7f;
                pImporter.loadInBackground = true;
                lSettings.preloadAudioData = false;
            }
            else if (lMusic)
            {
                lSettings.loadType = AudioClipLoadType.Streaming;
                lSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                lSettings.quality = 0.7f;
                pImporter.loadInBackground = true;
                lSettings.preloadAudioData = false;
            }
            else
            {
                lSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                lSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                lSettings.quality = 0.8f;
                pImporter.loadInBackground = false;
                lSettings.preloadAudioData = true;
            }
            lSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            pImporter.defaultSampleSettings = lSettings;
            pImporter.forceToMono = false;
        }

        // Re-imports every clip under the rules (after a change of the rules).
        public static int ReimportAll()
        {
            int lCount = 0;
            foreach (string lGuid in AssetDatabase.FindAssets("t:AudioClip", new[] { FOLDER }))
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuid);
                AssetDatabase.ImportAsset(lPath, ImportAssetOptions.ForceUpdate);
                lCount++;
            }
            return lCount;
        }
    }
}
