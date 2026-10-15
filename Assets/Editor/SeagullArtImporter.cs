using UnityEditor;
public class SeagullArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/SeagullArt/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = assetPath.Contains("arena_background") ? 1 : assetPath.Contains("octopus") ? 4 : assetPath.Contains("hero") || assetPath.Contains("/Cutout/gull/") ? 16 : 8;
        if (assetPath.Contains("/Cutout/"))
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
    }
}
