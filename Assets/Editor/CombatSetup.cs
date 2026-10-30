using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Sets up combat: sizes the rat and paper-ball sprites, creates the PaperBall and Rat_Plain
    /// prefabs (only if they don't exist yet, so later tweaks are kept), gives the Player prefab its
    /// Health, DamageFlash, PlayerHealth and PlayerShooter, and puts a test rat in MovementTest.
    /// Safe to run again.
    /// </summary>
    public static class CombatSetup
    {
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        const string PaperBallPrefabPath = "Assets/Prefabs/Combat/PaperBall.prefab";
        const string RatPrefabPath = "Assets/Prefabs/Enemies/Rat_Plain.prefab";
        const string PaperBallSpritePath = "Assets/Art/Placeholder/PaperBall.png";
        const string RatSpritePath = "Assets/Art/Rats/Rat_Plain.png";
        const string NoFrictionPath = "Assets/Physics/NoFriction.physicsMaterial2D";
        const string ScenePath = "Assets/Scenes/MovementTest.unity";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        const float RatHeight = 1.15f;
        const float PaperBallSize = 0.35f;

        [MenuItem("Tools/NoRegRats/Set Up Combat")]
        public static void Run()
        {
            Sprite ratSprite = ConfigureSprite(RatSpritePath, RatHeight, new Vector2(0.5f, 0f));
            Sprite ballSprite = ConfigureSprite(PaperBallSpritePath, PaperBallSize, new Vector2(0.5f, 0.5f));
            if (ratSprite == null || ballSprite == null)
            {
                Debug.LogError("CombatSetup: the rat or paper-ball sprite is missing.");
                return;
            }

            Projectile ball = AssetDatabase.LoadAssetAtPath<Projectile>(PaperBallPrefabPath) ?? CreatePaperBall(ballSprite);
            GameObject rat = AssetDatabase.LoadAssetAtPath<GameObject>(RatPrefabPath) ?? CreateRat(ratSprite);
            SetUpPlayer(ball);
            AddTestRat(rat);
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

        static GameObject CreateRat(Sprite sprite)
        {
            var root = new GameObject("Rat_Plain");
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

            Set(root.AddComponent<Health>(), "maxHealth", 2);
            Set(root.AddComponent<RatEnemy>(), "visual", visual.transform);
            Set(root.AddComponent<ContactDamage>(), "damage", 1);
            Set(root.AddComponent<DamageFlash>(), "target", renderer);

            EnsureFolder("Assets/Prefabs/Enemies");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, RatPrefabPath);
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

        static void AddTestRat(GameObject ratPrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (GameObject.Find("Test Rat") != null)
                return;

            var rat = (GameObject)PrefabUtility.InstantiatePrefab(ratPrefab, scene);
            rat.name = "Test Rat";
            rat.transform.position = new Vector3(-4f, 0.02f, 0f);
            Set(rat.GetComponent<RatEnemy>(), "patrolRange", 2.5f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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
