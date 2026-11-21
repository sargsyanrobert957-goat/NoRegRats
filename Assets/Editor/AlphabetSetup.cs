using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Makes (or refreshes) the DrawnAlphabet asset from the glyph pictures in Assets/Art/UI/Alphabet/
    /// (A.png ... Z.png, 0.png ... 9.png, Slash.png): white capitals 200 px tall, built from the
    /// artist's menu lettering. Used by MenuSetup (key boxes) and StartWallSetup (the start wall).
    /// </summary>
    public static class AlphabetSetup
    {
        const string Folder = "Assets/Art/UI/Alphabet/";
        const string AssetPath = Folder + "Alphabet.asset";
        const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/";

        internal static DrawnAlphabet Get()
        {
            var alphabet = AssetDatabase.LoadAssetAtPath<DrawnAlphabet>(AssetPath);
            if (alphabet == null)
            {
                alphabet = ScriptableObject.CreateInstance<DrawnAlphabet>();
                AssetDatabase.CreateAsset(alphabet, AssetPath);
            }
            var glyphs = new List<Sprite>();
            foreach (char c in Characters)
                glyphs.Add(Glyph(c == '/' ? "Slash" : c.ToString()));

            var so = new SerializedObject(alphabet);
            so.FindProperty("characters").stringValue = Characters;
            SerializedProperty list = so.FindProperty("glyphs");
            list.arraySize = glyphs.Count;
            for (int i = 0; i < glyphs.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = glyphs[i];
            so.FindProperty("capPixels").floatValue = 200f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return alphabet;
        }

        static Sprite Glyph(string name)
        {
            string path = Folder + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("AlphabetSetup: missing " + path);
                return null;
            }
            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            if (importer.textureType != TextureImporterType.Sprite || platform.maxTextureSize != 1024 || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                platform.maxTextureSize = 1024;
                importer.SetPlatformTextureSettings(platform);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
