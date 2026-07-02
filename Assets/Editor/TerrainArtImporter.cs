using UnityEditor;
using UnityEngine;

namespace Caveman
{
    /// <summary>Import settings for the hand-painted terrain tiles the bake samples: they must be
    /// CPU-readable (GetPixels at bake time), uncompressed (exact colours), mip-free and repeat-wrapped.
    /// Applies automatically to anything under Resources/art/terrain.</summary>
    public class TerrainArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("Resources/art/terrain")) return;
            var ti = (TextureImporter)assetImporter;
            ti.isReadable = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 1024;
        }
    }
}
