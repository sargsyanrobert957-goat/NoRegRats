using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Sets up combat: sizes the rat and paper-ball sprites, creates the PaperBall and rat prefabs
    /// (only if they don't exist yet, so later tweaks are kept), gives the Player prefab its Health,
    /// DamageFlash, PlayerHealth and PlayerShooter, and puts one rat of each type in MovementTest.
    /// Safe to run again.
    /// </summary>
    public static class CombatSetup
    {
        // One entry per rat type: the stats from the paper prototype's rat table.
        struct RatType
        {
            public string Name, TestName;
            public int Health, Bite;
            public float Speed, TestX, TestRange;
        }

        static readonly RatType[] RatTypes =
        {
            new RatType { Name = "Rat_Plain", TestName = "Test Rat", Health = 2, Bite = 1, Speed = 1.6f, TestX = -4f, TestRange = 2.5f },
            new RatType { Name = "Rat_RedCap", TestName = "Test Red-cap", Health = 3, Bite = 1, Speed = 1.9f, TestX = 6f, TestRange = 2.5f },
            new RatType { Name = "Rat_Jacket", TestName = "Test Jacket", Health = 4, Bite = 2, Speed = 1.3f, TestX = 21.5f, TestRange = 1.5f },
        };

        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        const string PaperBallPrefabPath = "Assets/Prefabs/Combat/PaperBall.prefab";
        const string RatPrefabFolder = "Assets/Prefabs/Enemies/";
        const string PaperBallSpritePath = "Assets/Art/Placeholder/PaperBall.png";
        const string RatSpriteFolder = "Assets/Art/Rats/";
        const string NoFrictionPath = "Assets/Physics/NoFriction.physicsMaterial2D";
        const string ScenePath = "Assets/Scenes/MovementTest.unity";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        const float RatHeight = 1.15f;
        const float PaperBallSize = 0.35f;

        [MenuItem("Tools/NoRegRats/Set Up Combat")]
        public static void Run()
        {
            Sprite ballSprite = ConfigureSprite(PaperBallSpritePath, PaperBallSize, new Vector2(0.5f, 0.5f));
            if (ballSprite == null)
            {
                Debug.LogError("CombatSetup: the paper-ball sprite is missing.");
                return;
            }
            Projectile ball = AssetDatabase.LoadAssetAtPath<Projectile>(PaperBallPrefabPath) ?? CreatePaperBall(ballSprite);
            SetUpPlayer(ball);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (RatType type in RatTypes)
            {
                Sprite sprite = ConfigureSprite(RatSpriteFolder + type.Name + ".png", RatHeight, new Vector2(0.5f, 0f));
                if (sprite == null)
                {
                    Debug.LogError($"CombatSetup: {RatSpriteFolder}{type.Name}.png is missing.");
                    continue;
                }
                string prefabPath = RatPrefabFolder + type.Name + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) ?? CreateRat(type, sprite, prefabPath);
                AddTestRat(prefab, type, scene);
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CombatSetup: done.");
        }

        static Projectile CreatePaperBall(Sprite sprite)
        {
            var go = new GameObject("PaperBall");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 15;
            ApplySpriteMaterial(renderer);

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = PaperBallSize * 0.4f;

            go.AddComponent<Projectile>();
            EnsureFolder("Assets/Prefabs/Combat");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PaperBallPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Projectile>();
        }

        static GameObject CreateRat(RatType type, Sprite sprite, string prefabPath)
        {
            var root = new GameObject(type.Name);
            var body = root.AddComponent<Rigidbody2D>();
            body.mass = 2f;
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // The root's origin is at the rat's feet, like the sprite's pivot.
            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.6f, 1f);
            collider.offset = new Vector2(0f, 0.5f);
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
            ApplySpriteMaterial(renderer);

            Set(root.AddComponent<Health>(), "maxHealth", type.Health);
            var rat = root.AddComponent<RatEnemy>();
            Set(rat, "visual", visual.transform);
            Set(rat, "walkSpeed", type.Speed);
            Set(root.AddComponent<ContactDamage>(), "damage", type.Bite);
            Set(root.AddComponent<DamageFlash>(), "target", renderer);

            EnsureFolder("Assets/Prefabs/Enemies");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void SetUpPlayer(Projectile ball)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var heroAnimator = root.GetComponentInChildren<HeroAnimator>();
                SpriteRenderer heroSprite = heroAnimator.GetComponent<SpriteRenderer>();

                if (!root.TryGetComponent(out Health health))
                {
                    health = root.AddComponent<Health>();
                    Set(health, "maxHealth", 3);
                    Set(health, "invulnerableTime", 1f);
                }
                if (!root.TryGetComponent(out DamageFlash flash))
                {
                    flash = root.AddComponent<DamageFlash>();
                    Set(flash, "target", heroSprite);
                    Set(flash, "blinkWhileInvulnerable", true);
                }
                if (!root.TryGetComponent(out PlayerHealth _))
                    root.AddComponent<PlayerHealth>();
                if (!root.TryGetComponent(out PlayerShooter shooter))
                    shooter = root.AddComponent<PlayerShooter>();
                Set(shooter, "projectilePrefab", ball);
                Set(shooter, "heroAnimator", heroAnimator);
                Set(shooter, "muzzleOffset", new Vector2(0.45f, -0.15f));

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void AddTestRat(GameObject ratPrefab, RatType type, UnityEngine.SceneManagement.Scene scene)
        {
            if (GameObject.Find(type.TestName) != null)
                return;

            var rat = (GameObject)PrefabUtility.InstantiatePrefab(ratPrefab, scene);
            rat.name = type.TestName;
            rat.transform.position = new Vector3(type.TestX, 0.02f, 0f);
            Set(rat.GetComponent<RatEnemy>(), "patrolRange", type.TestRange);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // Imports a sprite so it is `worldSize` units tall, with the given pivot.
        static Sprite ConfigureSprite(string path, float worldSize, Vector2 pivot)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = height / worldSize;
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
                case Vector2 v: property.vector2Value = v; break;
                case Object o: property.objectReferenceValue = o; break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
