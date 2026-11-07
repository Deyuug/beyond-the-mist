using UnityEditor;
using UnityEngine;

namespace ARR.EditorTools
{
    /// Background music is long: stream it from disk (compressed) instead of decompressing it into memory.
    public class ArrAudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/ARR/Music/")) return;
            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            importer.defaultSampleSettings = s;
        }
    }
}
