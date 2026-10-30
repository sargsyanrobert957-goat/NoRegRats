using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Builds the first version of Level1 from reusable prefabs: the level-piece prefabs in
    /// Assets/Prefabs/Level/ (created if missing), then the scene itself, made from a copy of
    /// MovementTest's camera, light and player. After that the scene is edited by hand in Unity like
    /// any other: this only builds a Level1 that doesn't exist yet, and never overwrites one.
    ///
    /// Layout (world units; the subway floor's top is y = 0, the sewers' is y = -20):
    /// subway start → train cars → crate to push against a wall too high to jump → drop down the
    /// "SEWERS" shaft at x 62–66 → sewers with a crouch-only gap under pipes → AC-unit steps →
    /// the boss arena at x 112–134 (its gate, the Rat King and the exit come in a later milestone).
    /// </summary>
    public static class Level1Builder
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string TemplateScenePath = "Assets/Scenes/MovementTest.unity";
        const string LevelPrefabFolder = "Assets/Prefabs/Level/";
        const string EnemyPrefabFolder = "Assets/Prefabs/Enemies/";
        const string LevelArtFolder = "Assets/Art/Level/";
        const string SquareSpritePath = "Assets/Sprites/Placeholder/Square.png";
        const string CrateSpritePath = "Assets/Sprites/Placeholder/Crate.png";
        const string GraphPaperPath = "Assets/Art/Background/GraphPaper.png";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        static readonly Color Ink = new Color(0.17f, 0.17f, 0.2f);
        static readonly Rect LevelBounds = Rect.MinMaxRect(-6f, -24f, 136f, 14f);

        // A piece of level art: its sprite's width in the world, and the part of the sprite that is
        // solid (as fractions of its size, from the bottom-left) or no collider at all.
        struct PieceType
        {
            public string Name;
            public float Width;
            public Rect Solid;
            public bool HasCollider;
            public int SortingOrder;
        }

        static readonly PieceType[] PieceTypes =
        {
            new PieceType { Name = "TrainCar", Width = 7f, Solid = new Rect(0.01f, 0.05f, 0.98f, 0.95f), HasCollider = true },
            new PieceType { Name = "StoneBridge", Width = 3f, Solid = new Rect(0f, 0.55f, 1f, 0.45f), HasCollider = true },
            new PieceType { Name = "BrickLedge", Width = 3.5f, Solid = new Rect(0f, 0f, 1f, 1f), HasCollider = true },
            new PieceType { Name = "ACUnit", Width = 2f, Solid = new Rect(0f, 0.45f, 1f, 0.55f), HasCollider = true },
            new PieceType { Name = "Pipe", Width = 1f, Solid = new Rect(0f, 0f, 1f, 1f), HasCollider = true },
            new PieceType { Name = "ScaffoldBar", Width = 3f, Solid = new Rect(0f, 0f, 1f, 1f), HasCollider = true },
            new PieceType { Name = "Sign_Sewers", Width = 1f, HasCollider = false, SortingOrder = -10 },
            new PieceType { Name = "Sign_Subway", Width = 1f, HasCollider = false, SortingOrder = -10 },
        };

        [MenuItem("Tools/NoRegRats/Build Level1")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.LogWarning("Level1Builder: Level1 already exists, so nothing was built. Edit it in the scene, or delete it first to rebuild.");
                return;
            }

            EnsureFolder("Assets/Prefabs/Level");
            foreach (PieceType type in PieceTypes)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabFolder + type.Name + ".prefab") == null)
                    CreatePiecePrefab(type);
            GameObject block = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabFolder + "Block.prefab") ?? CreateBlockPrefab();
            GameObject crate = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabFolder + "Crate.prefab") ?? CreateCratePrefab();
            Sprite paper = ConfigureGraphPaper();

            AssetDatabase.CopyAsset(TemplateScenePath, ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var camera = Object.FindAnyObjectByType<CameraFollow2D>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bool keep = root == player.gameObject || root == camera.gameObject || root.GetComponent<UnityEngine.Rendering.Universal.Light2D>() != null;
                if (!keep)
                    Object.DestroyImmediate(root);
            }

            var level = new GameObject("Level").transform;
            BuildSubway(Group("Subway", level), block, crate);
            BuildShaft(Group("Shaft", level), block);
            BuildSewers(Group("Sewers", level), block);
            BuildArena(Group("Arena", level), block);

            player.transform.position = new Vector3(2f, 0.95f, 0f);
            camera.transform.position = new Vector3(4.7f, 2.5f, camera.transform.position.z);
            Set(camera, "bounds", LevelBounds);
            AddBackground(paper, camera.transform);

            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(TemplateScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Level1Builder: Level1 built.");
        }

        static void BuildSubway(Transform parent, GameObject block, GameObject crate)
        {
            Block(block, parent, "Floor", -6f, -12f, 62f, 0f);
            Block(block, parent, "Left Wall", -6f, 0f, -4f, 14f);
            Piece("Sign_Subway", parent, 0f, 0f, onGround: true);

            Piece("BrickLedge", parent, 6f, 1.4f);
            Piece("TrainCar", parent, 12f, 0f, onGround: true);
            Rat("Rat_Plain", parent, 12f, Top("TrainCar", 0f), 2.5f);
            Piece("TrainCar", parent, 21f, 0f, onGround: true);
            Piece("StoneBridge", parent, 27.5f, 5f);
            Rat("Rat_RedCap", parent, 28.5f, 0f, 2.5f);

            // The crate puzzle: the wall's top (4.2) is out of jumping reach from the floor (3.5) but
            // not from the crate (1 + 3.5). The low stopper keeps the crate from being pushed too far
            // back to push it again.
            Block(block, parent, "Crate Stopper", 31f, 0f, 31.6f, 0.6f);
            Instantiate(crate, parent, "Crate", new Vector3(34f, 0.5f, 0f));
            Block(block, parent, "Crate Wall", 40f, 0f, 44f, 4.2f);

            Rat("Rat_Plain", parent, 50f, 0f, 3f);
            Piece("ScaffoldBar", parent, 54f, 3f);
            Piece("Sign_Sewers", parent, 60.2f, 0f, onGround: true);
        }

        static void BuildShaft(Transform parent, GameObject block)
        {
            // Everything right of the shaft above the sewers is solid ground.
            Block(block, parent, "Right Ground", 66f, -10f, 136f, 14f);
        }

        static void BuildSewers(Transform parent, GameObject block)
        {
            Block(block, parent, "Floor", 54f, -24f, 136f, -20f);
            Block(block, parent, "Left Wall", 54f, -20f, 56f, -12f);

            Piece("BrickLedge", parent, 71f, -17.6f);
            Rat("Rat_Jacket", parent, 77f, -20f, 2.5f);

            // The crouch gap: pipes 1.4 above the floor (the hero is 1.8 standing, 1.2 crouched),
            // with solid ground above them up to the ceiling.
            Block(block, parent, "Crouch Gap Fill", 84f, -17.6f, 90.4f, -10f);
            for (int i = 0; i < 3; i++)
            {
                GameObject pipe = Piece("Pipe", parent, 0f, 0f);
                pipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                float length = SpriteSize("Pipe").y;
                pipe.transform.position = new Vector3(84f + length * (i + 0.5f), -18.1f, 0f);
                pipe.name = "Crouch Gap Pipe";
            }

            Piece("ACUnit", parent, 94f, -18.2f);
            Piece("ACUnit", parent, 98f, -16.2f);
            Piece("BrickLedge", parent, 104f, -14.8f);
            Rat("Rat_Jacket", parent, 104f, -14.8f, 1.1f);
        }

        static void BuildArena(Transform parent, GameObject block)
        {
            Block(block, parent, "Right Wall", 134f, -20f, 136f, -10f);
            Piece("ScaffoldBar", parent, 117f, -16.5f);
            Piece("ScaffoldBar", parent, 129f, -16.5f);
            // Markers for the boss fight, filled in by the next milestone.
            Marker(parent, "Arena Gate Spot", new Vector2(112f, -18f));
            Marker(parent, "Rat King Spot", new Vector2(127f, -20f));
            Marker(parent, "Exit Spot", new Vector2(133.5f, -19f));
        }

        // --- Building blocks ---

        static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static void Block(GameObject prefab, Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            GameObject block = Instantiate(prefab, parent, name, new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f));
            block.transform.localScale = new Vector3(xMax - xMin, yMax - yMin, 1f);
        }

        // Places a piece by its top edge, or standing on the ground at groundY when onGround is set.
        static GameObject Piece(string name, Transform parent, float x, float y, bool onGround = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabFolder + name + ".prefab");
            float height = SpriteSize(name).y;
            float centerY = onGround ? y + height * 0.5f : y - height * 0.5f;
            return Instantiate(prefab, parent, name, new Vector3(x, centerY, 0f));
        }

        static float Top(string pieceName, float groundY) => groundY + SpriteSize(pieceName).y;

        static void Rat(string name, Transform parent, float x, float groundY, float patrolRange)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabFolder + name + ".prefab");
            GameObject rat = Instantiate(prefab, parent, name, new Vector3(x, groundY + 0.02f, 0f));
            Set(rat.GetComponent<RatEnemy>(), "patrolRange", patrolRange);
        }

        static void Marker(Transform parent, string name, Vector2 position)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = position;
        }

        static GameObject Instantiate(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            return instance;
        }

        static Vector2 SpriteSize(string pieceName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(LevelArtFolder + pieceName + ".png").bounds.size;

        static void AddBackground(Sprite paper, Transform cameraTransform)
        {
            var background = new GameObject("Background");
            background.transform.position = cameraTransform.position + new Vector3(25f, -3f, 0f);
            background.transform.position = new Vector3(background.transform.position.x, background.transform.position.y, 5f);
            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = paper;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(110f, 44f);
            renderer.sortingOrder = -100;
            ApplySpriteMaterial(renderer);

            var parallax = background.AddComponent<ParallaxBackground>();
            var tint = new Gradient();
            tint.SetKeys(
                new[] { new GradientColorKey(new Color(0.86f, 0.93f, 0.86f), 0f), new GradientColorKey(new Color(0.9f, 0.93f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var serialized = new SerializedObject(parallax);
            serialized.FindProperty("cameraTransform").objectReferenceValue = cameraTransform;
            serialized.FindProperty("tint").gradientValue = tint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Prefabs ---

        static void CreatePiecePrefab(PieceType type)
        {
            string spritePath = LevelArtFolder + type.Name + ".png";
            Sprite sprite = ConfigureSprite(spritePath, type.Width);
            if (sprite == null)
            {
                Debug.LogError("Level1Builder: missing " + spritePath);
                return;
            }

            var go = new GameObject(type.Name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = type.SortingOrder;
            ApplySpriteMaterial(renderer);
            if (type.HasCollider)
            {
                Vector2 size = sprite.bounds.size;
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(type.Solid.width * size.x, type.Solid.height * size.y);
                collider.offset = new Vector2((type.Solid.center.x - 0.5f) * size.x, (type.Solid.center.y - 0.5f) * size.y);
            }
            PrefabUtility.SaveAsPrefabAsset(go, LevelPrefabFolder + type.Name + ".prefab");
            Object.DestroyImmediate(go);
        }

        static GameObject CreateBlockPrefab()
        {
            var go = new GameObject("Block");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            renderer.color = Ink;
            ApplySpriteMaterial(renderer);
            go.AddComponent<BoxCollider2D>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, LevelPrefabFolder + "Block.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject CreateCratePrefab()
        {
            var go = new GameObject("Crate");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrateSpritePath);
            renderer.sortingOrder = 1;
            ApplySpriteMaterial(renderer);
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<PushableBox>().ApplyDefaultBodySettings();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, LevelPrefabFolder + "Crate.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // Level art: centred pivot, sized by width.
        static Sprite ConfigureSprite(string path, float worldWidth)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;
            importer.GetSourceTextureWidthAndHeight(out int width, out int _);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = width / worldWidth;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Tiles seamlessly: 128 px per unit (one grid cell), full-rect mesh, repeating texture.
        static Sprite ConfigureGraphPaper()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(GraphPaperPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(GraphPaperPath);
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

        static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            switch (value)
            {
                case float f: property.floatValue = f; break;
                case Rect r: property.rectValue = r; break;
                case Object o: property.objectReferenceValue = o; break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
