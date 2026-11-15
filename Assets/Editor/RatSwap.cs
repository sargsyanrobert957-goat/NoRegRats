using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoRegRats
{
    /// <summary>
    /// One-time swap to the team artist's final rats: every plain and red-cap rat in Level1 and
    /// MovementTest becomes Rat1 (the chain rat), every jacket rat becomes Rat3 (the machete rat, with the old jacket rat's stats). Rat2 (the gunner) stays where it is but now does 2 damage with
    /// both its bullet and its bite. Creates the Rat3 prefab if missing, renames Rat_Thug to Rat1 and
    /// sets its stats. Safe to run again. Delete this file once it has been run and committed.
    /// </summary>
    public static class RatSwap
    {
        const string Rat3Folder = "Assets/Art/Rats/Rat3/";
        const string EnemyFolder = "Assets/Prefabs/Enemies/";
        const string Rat1Path = EnemyFolder + "Rat1.prefab";
        const string OldRat1Path = EnemyFolder + "Rat_Thug.prefab";
        const string Rat3Path = EnemyFolder + "Rat3.prefab";
        const string GunnerPath = EnemyFolder + "Rat_Gunner.prefab";
        const string BulletPath = "Assets/Prefabs/Combat/Bullet.prefab";
        const string NoFrictionPath = "Assets/Physics/NoFriction.physicsMaterial2D";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        // Rat1 replaces the plain (2 HP) and red-cap (3 HP) rats; Rat3 replaces the jacket rat (4 HP, bite 2).
        const int Rat1Health = 3;
        const int Rat1Bite = 1;
        const float Rat1Speed = 1.8f;
        const int Rat3Health = 4;
        const int Rat3Bite = 2;
        const float Rat3Speed = 1.3f;
        // Same as the old jacket rat: it bites on touching (ContactDamage's default reach).
        const float Rat3Reach = 0.02f;
        const int Rat2Damage = 2;

        // The drawings' pixels per world unit (Rat3's frames are 1812 to 1850 px tall; this makes the
        // standing frame 1.9 units tall) and the feet pivot (fractions of each canvas).
        const float Rat3PixelsPerUnit = 1812f / 1.9f;
        const float Rat3PivotX = 0.607f;

        [MenuItem("Tools/NoRegRats/Replace Rats With Rat1 And Rat3")]
        public static void Run()
        {
            if (!SetUpRat3Art(out var frames, out Sprite standing, out Sprite hit))
                return;

            GameObject rat1 = SetUpRat1();
            GameObject rat3 = AssetDatabase.LoadAssetAtPath<GameObject>(Rat3Path) ?? CreateRat3(standing, frames, hit);
            SetUpRat2();
            if (rat1 == null || rat3 == null)
                return;
            SetUpRat3Stats();

            foreach (string scenePath in new[] { "Assets/Scenes/Level1.unity", "Assets/Scenes/MovementTest.unity" })
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath);
                int swapped = 0;
                swapped += Replace("Assets/Prefabs/Enemies/Rat_Plain.prefab", rat1, scene);
                swapped += Replace("Assets/Prefabs/Enemies/Rat_RedCap.prefab", rat1, scene);
                swapped += Replace("Assets/Prefabs/Enemies/Rat_Jacket.prefab", rat3, scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"RatSwap: {scenePath}: {swapped} rat(s) replaced.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("RatSwap: done.");
        }

        // ---- Rat3 art ----

        static bool SetUpRat3Art(out Sprite[] frames, out Sprite standing, out Sprite hit)
        {
            frames = new Sprite[5];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = Configure(Rat3Folder + "Rat3_Anim_" + (i + 1) + ".png", 0.0292f);
            standing = Configure(Rat3Folder + "Rat3_Standing.png", 0.0298f);
            hit = Configure(Rat3Folder + "Rat3_Hit.png", 0.0298f);
            if (standing == null || hit == null || System.Array.IndexOf(frames, null) >= 0)
            {
                Debug.LogError("RatSwap: the Rat3 drawings are missing from Assets/Art/Rats/Rat3/.");
                return false;
            }
            return true;
        }

        static Sprite Configure(string path, float pivotY)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Rat3PixelsPerUnit;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(Rat3PivotX, pivotY);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---- Prefabs ----

        // The chain rat prefab becomes the regular rat, Rat1, with the stats of the rats it replaces.
        static GameObject SetUpRat1()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Rat1Path) == null)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(OldRat1Path) == null)
                {
                    Debug.LogError("RatSwap: Rat_Thug.prefab (the Rat1 prefab) is missing. Run Set Up New Rats first.");
                    return null;
                }
                string error = AssetDatabase.RenameAsset(OldRat1Path, "Rat1");
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning("RatSwap: couldn't rename Rat_Thug to Rat1: " + error);
            }

            string path = AssetDatabase.LoadAssetAtPath<GameObject>(Rat1Path) != null ? Rat1Path : OldRat1Path;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Set(root.GetComponent<Health>(), "maxHealth", Rat1Health);
                Set(root.GetComponent<RatEnemy>(), "walkSpeed", Rat1Speed);
                Set(root.GetComponent<ContactDamage>(), "damage", Rat1Bite);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static GameObject CreateRat3(Sprite standing, Sprite[] frames, Sprite hit)
        {
            var root = new GameObject("Rat3");
            var body = root.AddComponent<Rigidbody2D>();
            body.mass = 3f;
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // The origin is at the feet, like the sprite's pivot.
            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.65f, 1.55f);
            collider.offset = new Vector2(0f, 0.775f);
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = standing;
            renderer.sortingOrder = 5;
            // The drawing faces left; RatEnemy mirrors the visual to face the walking direction.
            renderer.flipX = true;
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteLitMaterialGuid));
            if (material != null)
                renderer.sharedMaterial = material;

            Set(root.AddComponent<Health>(), "maxHealth", Rat3Health);
            var rat = root.AddComponent<RatEnemy>();
            Set(rat, "visual", visual.transform);
            Set(rat, "walkSpeed", Rat3Speed);
            var bite = root.AddComponent<ContactDamage>();
            Set(bite, "damage", Rat3Bite);
            Set(bite, "reach", Rat3Reach);
            Set(root.AddComponent<DamageFlash>(), "target", renderer);

            var animator = root.AddComponent<RatAnimator>();
            Set(animator, "target", renderer);
            Set(animator, "framesPerSecond", 6f);
            var serialized = new SerializedObject(animator);
            SerializedProperty list = serialized.FindProperty("frames");
            list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Set(animator, "hitFrame", hit);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Rat3Path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Puts Rat3's stats on the prefab, also when it already exists from an earlier run.
        static void SetUpRat3Stats()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(Rat3Path);
            try
            {
                Set(root.GetComponent<Health>(), "maxHealth", Rat3Health);
                Set(root.GetComponent<RatEnemy>(), "walkSpeed", Rat3Speed);
                Set(root.GetComponent<ContactDamage>(), "damage", Rat3Bite);
                Set(root.GetComponent<ContactDamage>(), "reach", Rat3Reach);
                PrefabUtility.SaveAsPrefabAsset(root, Rat3Path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The gun rat does 2 damage, with its bullet and with its bite.
        static void SetUpRat2()
        {
            GameObject gunner = PrefabUtility.LoadPrefabContents(GunnerPath);
            try
            {
                Set(gunner.GetComponent<ContactDamage>(), "damage", Rat2Damage);
                PrefabUtility.SaveAsPrefabAsset(gunner, GunnerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(gunner);
            }

            GameObject bullet = PrefabUtility.LoadPrefabContents(BulletPath);
            try
            {
                Set(bullet.GetComponent<EnemyBullet>(), "damage", Rat2Damage);
                PrefabUtility.SaveAsPrefabAsset(bullet, BulletPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(bullet);
            }
        }

        // ---- Scenes ----

        // Replaces every instance of the old rat prefab in the open scene with one of the new prefab,
        // in the same place (parent, position, order, name, patrol range and starting direction).
        static int Replace(string oldPrefabPath, GameObject newPrefab, Scene scene)
        {
            int count = 0;
            foreach (RatEnemy rat in Object.FindObjectsByType<RatEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(rat.gameObject);
                if (source == null || AssetDatabase.GetAssetPath(source) != oldPrefabPath)
                    continue;

                var old = new SerializedObject(rat);
                float patrol = old.FindProperty("patrolRange").floatValue;
                int direction = old.FindProperty("startDirection").intValue;
                Transform oldTransform = rat.transform;
                Transform parent = oldTransform.parent;
                int sibling = oldTransform.GetSiblingIndex();
                Vector3 position = oldTransform.position;
                string name = oldTransform.name;

                var fresh = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab, scene);
                fresh.transform.SetParent(parent, true);
                fresh.transform.position = position;
                fresh.transform.SetSiblingIndex(sibling);
                fresh.name = name;
                Set(fresh.GetComponent<RatEnemy>(), "patrolRange", patrol);
                Set(fresh.GetComponent<RatEnemy>(), "startDirection", direction);

                Object.DestroyImmediate(rat.gameObject);
                count++;
            }
            return count;
        }

        // Sets a private [SerializeField] through Unity's serialization.
        static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            switch (value)
            {
                case int i: property.intValue = i; break;
                case float f: property.floatValue = f; break;
                case bool b: property.boolValue = b; break;
                case Object o: property.objectReferenceValue = o; break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
