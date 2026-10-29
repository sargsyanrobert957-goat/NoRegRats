using UnityEditor;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Gives every image dropped into Assets/Art/ sprite import settings the first time it is
    /// imported: a single sprite with its pivot at the bottom centre, sized at the hero's drawing
    /// scale, and capped at 512 px so big drawings don't waste memory. Settings changed by hand later
    /// in the Inspector are kept, and replacing a PNG with a file of the same name keeps them too.
    /// </summary>
    class ArtImportSettings : AssetPostprocessor
    {
        const string ArtFolder = "Assets/Art/";

        // The hero is drawn about 1570 px tall and should be 1.8 units, the height of its collider.
        public const float PixelsPerUnit = 870f;
        const int MaxSize = 512;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder) || !assetImporter.importSettingsMissing)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.maxTextureSize = MaxSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
        }
    }
}
