using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    public class AudioImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (AudioImporter)assetImporter;

            bool streamed = assetPath.StartsWith(Root + "Music/") || assetPath.StartsWith(Root + "Sfx/Ambience/");
            bool positional = assetPath.StartsWith(Root + "Sfx/Selection/") || assetPath.StartsWith(Root + "Sfx/Voice/")
                || assetPath.StartsWith(Root + "Sfx/Impacts/")
                || assetPath.StartsWith(Root + "Sfx/Culture/");

            importer.forceToMono = positional;
            importer.loadInBackground = streamed;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = streamed ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = streamed ? 0.6f : 0.7f;
            importer.defaultSampleSettings = settings;
        }
    }
}
