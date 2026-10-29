using UnityEditor;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Puts the drawn hero on the Player prefab: sets the hero frames' import settings (feet pivot,
    /// size, 512 px cap), clears whatever was on the "Visual" child before, and gives it a
    /// SpriteRenderer and a HeroAnimator wired to the frames. Safe to run again, e.g. after
    /// replacing the frames with new drawings of the same names.
    /// </summary>
    public static class HeroSetup
    {
        const string HeroFolder = "Assets/Art/Hero/";
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        const int RunFrameCount = 8;
        // URP's Sprite-Lit-Default material, the one the level's sprites use.
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        // Where the feet are on each canvas, as a fraction of its size (measured from the drawings).
        // All run frames share one pivot so the artist's up-and-down bounce is kept.
        static readonly Vector2 RunPivot = new Vector2(0.5f, 0.011f);
        static readonly Vector2 IdlePivot = new Vector2(0.49f, 0.032f);

        [MenuItem("Tools/NoRegRats/Set Up Hero Sprite")]
        public static void Run()
        {
            Sprite idle = ConfigureSprite(HeroFolder + "Hero_Idle.png", IdlePivot);
            var runFrames = new Sprite[RunFrameCount];
            for (int i = 0; i < RunFrameCount; i++)
                runFrames[i] = ConfigureSprite($"{HeroFolder}Hero_Run_{i + 1}.png", RunPivot);
            if (idle == null || System.Array.IndexOf(runFrames, null) >= 0)
            {
                Debug.LogError("HeroSetup: some hero frames are missing from " + HeroFolder);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    visual = new GameObject("Visual").transform;
                    visual.SetParent(root.transform, false);
                }

                // Remove the old stick figure: its line children and its (now deleted) animator script.
                for (int i = visual.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(visual.GetChild(i).gameObject);
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(visual.gameObject);
                foreach (LineRenderer line in visual.GetComponents<LineRenderer>())
                    Object.DestroyImmediate(line);

                // The sprites' pivots are at the feet, so Visual sits at the bottom of the collider.
                var capsule = root.GetComponent<CapsuleCollider2D>();
                visual.localPosition = new Vector3(0f, capsule.offset.y - capsule.size.y * 0.5f, 0f);
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;

                if (!visual.TryGetComponent(out SpriteRenderer spriteRenderer))
                    spriteRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = idle;
                spriteRenderer.sortingOrder = 10;
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteLitMaterialGuid));
                if (material != null)
                    spriteRenderer.sharedMaterial = material;

                if (!visual.TryGetComponent(out HeroAnimator animator))
                    animator = visual.gameObject.AddComponent<HeroAnimator>();
                var serialized = new SerializedObject(animator);
                serialized.FindProperty("movement").objectReferenceValue = root.GetComponent<PlayerMovement>();
                serialized.FindProperty("idleFrame").objectReferenceValue = idle;
                SerializedProperty frames = serialized.FindProperty("runFrames");
                frames.arraySize = runFrames.Length;
                for (int i = 0; i < runFrames.Length; i++)
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = runFrames[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("HeroSetup: the Player prefab now uses the drawn hero.");
        }

        static Sprite ConfigureSprite(string path, Vector2 pivot)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ArtImportSettings.PixelsPerUnit;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
