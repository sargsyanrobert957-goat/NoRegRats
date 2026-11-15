using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Sets up the two new rats from the team artist's drawings: Rat1 (the chain rat, a tough rat
    /// with an animation loop) and Rat2 (the gun rat that shoots, with its bullet). Sizes their
    /// sprites, creates the prefabs if missing (so later tweaks are kept), and puts them in a scene:
    /// MovementTest gets one of each for testing; Level1 gets the chain rat in the sewers and the gun
    /// rat just before the boss arena. Only adds what's missing, so it is safe to run again.
    /// </summary>
    public static class RatSetup
    {
        const string Rat1Folder = "Assets/Art/Rats/Rat1/";
        const string Rat2Folder = "Assets/Art/Rats/Rat2/";
        const string BulletSpritePath = "Assets/Art/Rats/Rat2/Bullet.png";
        const string ThugPrefabPath = "Assets/Prefabs/Enemies/Rat_Thug.prefab";
        const string GunnerPrefabPath = "Assets/Prefabs/Enemies/Rat_Gunner.prefab";
        const string BulletPrefabPath = "Assets/Prefabs/Combat/Bullet.prefab";
        const string NoFrictionPath = "Assets/Physics/NoFriction.physicsMaterial2D";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        // The drawings' canvases are 1812 px tall with the rat filling almost all of it, so the
        // canvas height in world units sets how tall the rat is (the hero is 1.8, other rats 1.15).
        const float Rat1CanvasHeight = 1.8f;
        const float Rat2CanvasHeight = 1.65f;
        const float BulletSize = 0.45f;
        // Feet on the sprite's bottom edge and centred under the body (fractions of the canvas).
        static readonly Vector2 Rat1Pivot = new Vector2(0.435f, 0.0215f);
        static readonly Vector2 Rat2Pivot = new Vector2(0.688f, 0.0221f);
        static readonly Vector2 Rat2HitPivot = new Vector2(0.579f, 0.0221f);

        // Rat stats: health, bite, walk speed.
        const int ThugHealth = 5;
        const int ThugBite = 2;
        const float ThugSpeed = 1.1f;
        const int GunnerHealth = 3;
        const int GunnerBite = 1;

        // Where they go in Level1 (x only; the floor is found under it).
        const float ThugLevelX = 134f;
        const float ThugPatrol = 2f;
        const float GunnerLevelX = 163f;
        // The sewers and arena: floor top y -20, ceiling about -10, so the floor search starts between.
        const float LevelRayStartY = -11f;

        [MenuItem("Tools/NoRegRats/Set Up New Rats (MovementTest)")]
        public static void RunMovementTest()
        {
            if (!CreateAssets(out GameObject thug, out GameObject gunner))
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MovementTest.unity");
            Place(thug, "Test Thug", 12f, 12f, null, scene, patrol: 2f);
            Place(gunner, "Test Gunner", 22f, 12f, null, scene, patrol: 0f);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RatSetup: done for MovementTest.");
        }

        [MenuItem("Tools/NoRegRats/Add New Rats To Level1")]
        public static void RunLevel1()
        {
            if (!CreateAssets(out GameObject thug, out GameObject gunner))
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            GameObject sewers = GameObject.Find("Level/Sewers");
            Transform parent = sewers != null ? sewers.transform : null;
            Place(thug, "Rat Thug", ThugLevelX, LevelRayStartY, parent, scene, ThugPatrol);
            Place(gunner, "Rat Gunner", GunnerLevelX, LevelRayStartY, parent, scene, 0f);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RatSetup: done for Level1. Check both rats in the Scene view and nudge them if they sit on a prop.");
        }

        static bool CreateAssets(out GameObject thug, out GameObject gunner)
        {
            thug = null;
            gunner = null;

            // Sprites.
            var thugFrames = new List<Sprite>();
            for (int i = 1; i <= 8; i++)
                thugFrames.Add(Configure(Rat1Folder + "Rat1_Anim_" + i + ".png", Rat1CanvasHeight, Rat1Pivot));
            Sprite thugStanding = Configure(Rat1Folder + "Rat1_Standing.png", Rat1CanvasHeight, Rat1Pivot);
            Sprite thugHit = Configure(Rat1Folder + "Rat1_Hit.png", Rat1CanvasHeight, Rat1Pivot);

            Sprite gunnerStanding = Configure(Rat2Folder + "Rat2_Standing.png", Rat2CanvasHeight, Rat2Pivot);
            var shootFrames = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
                shootFrames.Add(Configure(Rat2Folder + "Rat2_Shoot_" + i + ".png", Rat2CanvasHeight, Rat2Pivot));
            Sprite gunnerHit = Configure(Rat2Folder + "Rat2_Hit.png", Rat2CanvasHeight, Rat2HitPivot);
            Sprite bulletSprite = Configure(BulletSpritePath, BulletSize, new Vector2(0.5f, 0.5f));

            if (thugStanding == null || thugHit == null || gunnerStanding == null || gunnerHit == null || bulletSprite == null
                || thugFrames.Contains(null) || shootFrames.Contains(null))
            {
                Debug.LogError("RatSetup: some of the Rat1 / Rat2 drawings are missing from Assets/Art/Rats/.");
                return false;
            }

            // Prefabs.
            EnemyBullet bullet = AssetDatabase.LoadAssetAtPath<EnemyBullet>(BulletPrefabPath) ?? CreateBullet(bulletSprite);
            thug = AssetDatabase.LoadAssetAtPath<GameObject>(ThugPrefabPath) ?? CreateThug(thugStanding, thugFrames, thugHit);
            gunner = AssetDatabase.LoadAssetAtPath<GameObject>(GunnerPrefabPath) ?? CreateGunner(gunnerStanding, shootFrames, gunnerHit, bullet);
            return true;
        }

        static EnemyBullet CreateBullet(Sprite sprite)
        {
            var go = new GameObject("Bullet");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 15;
            ApplySpriteMaterial(renderer);

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = false;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = BulletSize * 0.3f;

            go.AddComponent<EnemyBullet>();
            EnsureFolder("Assets/Prefabs/Combat");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, BulletPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<EnemyBullet>();
        }

        static GameObject CreateThug(Sprite standing, List<Sprite> frames, Sprite hit)
        {
            GameObject root = NewRatRoot("Rat_Thug", standing, new Vector2(0.7f, 1.45f), out SpriteRenderer renderer, out Transform visual);
            // The drawing faces left; RatEnemy mirrors the visual to face the walking direction.
            renderer.flipX = true;

            Set(root.AddComponent<Health>(), "maxHealth", ThugHealth);
            var rat = root.AddComponent<RatEnemy>();
            Set(rat, "visual", visual);
            Set(rat, "walkSpeed", ThugSpeed);
            Set(root.AddComponent<ContactDamage>(), "damage", ThugBite);
            Set(root.AddComponent<DamageFlash>(), "target", renderer);

            var animator = root.AddComponent<RatAnimator>();
            Set(animator, "target", renderer);
            SetObjects(animator, "frames", frames);
            Set(animator, "hitFrame", hit);

            return SaveRat(root, ThugPrefabPath);
        }

        static GameObject CreateGunner(Sprite standing, List<Sprite> shootFrames, Sprite hit, EnemyBullet bullet)
        {
            GameObject root = NewRatRoot("Rat_Gunner", standing, new Vector2(0.6f, 1.3f), out SpriteRenderer renderer, out Transform _);

            Set(root.AddComponent<Health>(), "maxHealth", GunnerHealth);
            var shooter = root.AddComponent<RatShooter>();
            Set(shooter, "bulletPrefab", bullet);
            Set(shooter, "spriteRenderer", renderer);
            Set(shooter, "standingFrame", standing);
            SetObjects(shooter, "shootFrames", shootFrames);
            Set(shooter, "hitFrame", hit);
            Set(root.AddComponent<ContactDamage>(), "damage", GunnerBite);
            Set(root.AddComponent<DamageFlash>(), "target", renderer);

            return SaveRat(root, GunnerPrefabPath);
        }

        // Rigidbody, collider (origin at the feet, like the sprite's pivot) and a Visual child.
        static GameObject NewRatRoot(string name, Sprite sprite, Vector2 colliderSize, out SpriteRenderer renderer, out Transform visual)
        {
            var root = new GameObject(name);
            var body = root.AddComponent<Rigidbody2D>();
            body.mass = 2f;
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = colliderSize;
            collider.offset = new Vector2(0f, colliderSize.y * 0.5f);
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            var child = new GameObject("Visual");
            child.transform.SetParent(root.transform, false);
            renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
            ApplySpriteMaterial(renderer);
            visual = child.transform;
            return root;
        }

        static GameObject SaveRat(GameObject root, string path)
        {
            EnsureFolder("Assets/Prefabs/Enemies");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // Puts a rat on the floor under x (the top surface found by a downward ray), unless one with
        // this name is already in the scene.
        static void Place(GameObject prefab, string name, float x, float rayStartY, Transform parent, UnityEngine.SceneManagement.Scene scene, float patrol)
        {
            if (GameObject.Find(name) != null)
                return;

            var rat = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            rat.name = name;
            if (parent != null)
                rat.transform.SetParent(parent, true);
            rat.transform.position = new Vector3(x, FindFloor(x, rayStartY), 0f);
            if (patrol > 0f && rat.TryGetComponent(out RatEnemy enemy))
                Set(enemy, "patrolRange", patrol);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"RatSetup: placed {name} at x {x}, y {rat.transform.position.y:0.##}.");
        }

        // The first solid surface under (x, rayStartY), searching downward.
        static float FindFloor(float x, float rayStartY)
        {
            Physics2D.SyncTransforms();
            bool hitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = false;
            float result = 0.02f;
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, rayStartY), Vector2.down, 60f);
            if (hit.collider != null)
                result = hit.point.y + 0.02f;
            else
                Debug.LogWarning($"RatSetup: no floor found under x {x}; placed at y 0. Move the rat onto the floor.");
            Physics2D.queriesHitTriggers = hitTriggers;
            return result;
        }

        // Imports a sprite so the whole drawing is `worldHeight` units tall, with the given pivot.
        static Sprite Configure(string path, float worldHeight, Vector2 pivot)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;
            importer.GetSourceTextureWidthAndHeight(out int _, out int height);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = height / worldHeight;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void ApplySpriteMaterial(SpriteRenderer renderer)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteLitMaterialGuid));
            if (material != null)
                renderer.sharedMaterial = material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
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

        static void SetObjects(Object target, string field, List<Sprite> values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
