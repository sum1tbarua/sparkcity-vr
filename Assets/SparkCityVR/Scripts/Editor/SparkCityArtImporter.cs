// Import settings for artwork placed in Assets/SparkCityVR/Resources/SparkCityArt/
// (e.g. welcome.png). Unity's default 2048 px limit would blur an 8K image, so this
// raises the limit to 8192 and uses high-quality compression. Test the 8K original on the
// Quest 3S before deciding whether a lower resolution is acceptable.
using UnityEditor;

namespace SparkCity.EditorTools
{
    public class SparkCityArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/SparkCityArt/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = true;   // smoother when seen from a distance / as a panorama
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapModeU = UnityEngine.TextureWrapMode.Repeat; // no seam where a 360 image wraps around
            importer.wrapModeV = UnityEngine.TextureWrapMode.Clamp;

            // Quest 3S (Android): ASTC 6x6 keeps an 8K picture at roughly 15-20 MB of memory
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 8192;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
        }
    }
}