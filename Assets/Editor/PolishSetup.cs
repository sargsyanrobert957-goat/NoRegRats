using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Adds the polish: HeroEffects on the Player prefab, and to Level1 and MovementTest an
    /// "Effects" object (PuffSpawner with the puff sprite) and an "Audio" object (Sfx and Music,
    /// each with its own AudioSource; their clip slots start empty). Only adds what's missing.
    /// </summary>
    public static class PolishSetup
    {
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        const string PuffSpritePath = "Assets/Art/Placeholder/Puff.png";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";
        static readonly string[] Scenes = { "Assets/Scenes/Level1.unity", "Assets/Scenes/MovementTest.unity" };

        [MenuItem("Tools/NoRegRats/Set Up Effects And Audio")]
        public static void Run()
        {
            Sprite puff = ConfigurePuffSprite();
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteLitMaterialGuid));

            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (!player.TryGetComponent(out HeroEffects _))
                {
                    player.AddComponent<HeroEffects>();
                    PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }

            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                if (Object.FindAnyObjectByType<PuffSpawner>() == null)
                {
                    var spawner = new GameObject("Effects").AddComponent<PuffSpawner>();
                    var serialized = new SerializedObject(spawner);
                    serialized.FindProperty("puffSprite").objectReferenceValue = puff;
                    serialized.FindProperty("material").objectReferenceValue = material;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (Object.FindAnyObjectByType<Sfx>() == null)
                {
                    var audio = new GameObject("Audio");
                    var sounds = new GameObject("Sound Effects", typeof(AudioSource));
                    sounds.transform.SetParent(audio.transform, false);
                    sounds.GetComponent<AudioSource>().playOnAwake = false;
                    sounds.AddComponent<Sfx>();
                    var music = new GameObject("Music", typeof(AudioSource));
                    music.transform.SetParent(audio.transform, false);
                    music.GetComponent<AudioSource>().playOnAwake = false;
                    music.AddComponent<Music>();
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("PolishSetup: done.");
        }

        // One world unit wide, centred.
        static Sprite ConfigurePuffSprite()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(PuffSpritePath);
            importer.GetSourceTextureWidthAndHeight(out int width, out int _);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = width;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(PuffSpritePath);
        }
    }
}
