using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Tools > NoRegRats > Set Up Checkpoints: makes the Checkpoint prefab (a stand-in sign from the
    /// team's drawings: Art/Level/Checkpoint.png, with CheckpointTick.png shown once reached) if it's
    /// missing, puts the four checkpoints in Level1 (the start of the street, the subway station, the
    /// sewers and just before the boss arena) under Level/Checkpoints if they aren't there yet, and
    /// adds the Checkpoint sound to the scene's sound list.
    /// </summary>
    public static class CheckpointSetup
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string PrefabPath = "Assets/Prefabs/Level/Checkpoint.prefab";
        const string SignArt = "Assets/Art/Level/Checkpoint.png";
        const string TickArt = "Assets/Art/Level/CheckpointTick.png";
        const string SoundClip = "Assets/Audio/SFX/Heart.ogg";

        // Where the tick sits on the sign (its pivot is its bottom): centred in the board's frame.
        // In the sign's units, from its feet.
        static readonly Vector2 TickSpot = new Vector2(0f, 1.293f);

        // Index, where the sign stands (on the floor), and the goal shown on respawning there.
        static readonly (Vector2 spot, string goal)[] Spots =
        {
            (new Vector2(0.8f, 0f), ""),                       // the street, where the run starts
            (new Vector2(66.5f, -8f), ""),                     // the station, just past where you land
            (new Vector2(121f, -20f), "Find the Rat King"),    // the bottom of the shaft into the sewers
            (new Vector2(166f, -20f), "Find the Rat King"),    // just before the arena gate
        };

        // In front of the backgrounds (the station's walls), behind props (2), rats (5) and the hero (10).
        const int SignOrder = 1;
        // The drawing is 1.6 units tall; scaled so the board stands above the hero's head (1.8).
        const float SignScale = 1.4f;

        [MenuItem("Tools/NoRegRats/Set Up Checkpoints")]
        public static void Run()
        {
            // Full size: Assets/Art's 512 px cap would shrink the sign to a third of its height.
            KeepFullSize(SignArt);
            KeepFullSize(TickArt);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ?? MakePrefab();
            FixPrefab(prefab);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            Transform level = GameObject.Find("Level").transform;
            Transform group = level.Find("Checkpoints");
            if (group == null)
            {
                group = new GameObject("Checkpoints").transform;
                group.SetParent(level, false);
                for (int i = 0; i < Spots.Length; i++)
                {
                    var checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                    checkpoint.name = "Checkpoint " + i;
                    checkpoint.transform.position = Spots[i].spot;
                    var so = new SerializedObject(checkpoint.GetComponent<Checkpoint>());
                    so.FindProperty("index").intValue = i;
                    so.FindProperty("respawnGoal").stringValue = Spots[i].goal;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            AddSound();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("CheckpointSetup: done.");
        }

        static GameObject MakePrefab()
        {
            var root = new GameObject("Checkpoint");
            root.transform.localScale = Vector3.one * SignScale;
            var sign = root.AddComponent<SpriteRenderer>();
            sign.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SignArt);
            sign.sortingOrder = SignOrder;
            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1f, 4f);
            trigger.offset = new Vector2(0f, 2f);

            var tickObject = new GameObject("Tick");
            tickObject.transform.SetParent(root.transform, false);
            tickObject.transform.localPosition = TickSpot;
            var tick = tickObject.AddComponent<SpriteRenderer>();
            tick.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TickArt);
            tick.sortingOrder = SignOrder + 1;

            var checkpoint = root.AddComponent<Checkpoint>();
            var so = new SerializedObject(checkpoint);
            so.FindProperty("tick").objectReferenceValue = tick;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void KeepFullSize(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            // ArtImportSettings' cap is on the default platform settings.
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            if (settings.maxTextureSize == 2048)
                return;
            settings.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // Keeps a sign made earlier up to date: the sorting (they once sat behind the station's
        // walls), the size, and the tick's place on the redrawn board.
        static void FixPrefab(GameObject prefab)
        {
            var sign = prefab.GetComponent<SpriteRenderer>();
            var tick = prefab.transform.Find("Tick").GetComponent<SpriteRenderer>();
            if (sign.sortingOrder == SignOrder && tick.sortingOrder == SignOrder + 1 && prefab.transform.localScale.x == SignScale
                && (Vector2)tick.transform.localPosition == TickSpot)
                return;
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            root.transform.localScale = Vector3.one * SignScale;
            root.transform.Find("Tick").localPosition = TickSpot;
            root.GetComponent<SpriteRenderer>().sortingOrder = SignOrder;
            root.transform.Find("Tick").GetComponent<SpriteRenderer>().sortingOrder = SignOrder + 1;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // A stand-in: the Heart's chime, until there's a sound of its own.
        static void AddSound()
        {
            var sfx = Object.FindAnyObjectByType<Sfx>();
            if (sfx == null)
                return;
            var so = new SerializedObject(sfx);
            SerializedProperty sounds = so.FindProperty("sounds");
            for (int i = 0; i < sounds.arraySize; i++)
                if (sounds.GetArrayElementAtIndex(i).FindPropertyRelative("sound").intValue == (int)Sfx.Sound.Checkpoint)
                    return;
            sounds.arraySize++;
            SerializedProperty entry = sounds.GetArrayElementAtIndex(sounds.arraySize - 1);
            entry.FindPropertyRelative("sound").intValue = (int)Sfx.Sound.Checkpoint;
            SerializedProperty clips = entry.FindPropertyRelative("clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundClip);
            entry.FindPropertyRelative("volume").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
