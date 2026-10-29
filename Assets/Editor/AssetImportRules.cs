using UnityEditor;
using UnityEngine;

namespace BustAKlud.Editor
{
    /// <summary>
    /// Applies consistent import settings to the art and audio under Assets/Resources so a fresh
    /// checkout imports correctly without hand-edited .meta files.
    /// </summary>
    public class AssetImportRules : AssetPostprocessor
    {
        const string ArtPath = "Assets/Resources/Art/";
        const string AudioPath = "Assets/Resources/Audio/";

        // Bump when the rules change so existing assets are re-imported with them.
        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtPath))
                return;

            var importer = (TextureImporter)assetImporter;

            // The cannon art is read at runtime to cut the skull emblem out as a tintable overlay.
            importer.isReadable = assetPath.EndsWith("/cannon.png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioPath))
                return;

            // Short one-shots: keep them as PCM in memory so they start instantly and can be trimmed at runtime.
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
        }
    }
}
