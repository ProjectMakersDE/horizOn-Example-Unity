using UnityEditor;
public class SeagullArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/SeagullArt/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = assetPath.Contains("arena_background") ? 1 : assetPath.Contains("octopus") ? 4 : assetPath.Contains("hero") ? 16 : assetPath.Contains("pickup") ? 8 : 8;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
    }
}
