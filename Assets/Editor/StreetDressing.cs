using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Dresses Level1's first section with the team's street art (Assets/Art/Street/): sidewalk slabs
    /// and the road strip over the plain floor, a power box, floating sidewalk chunks, fire
    /// hydrants, a pushable dumpster and a big power box as the wall in the crate puzzle, replacing
    /// the stand-in pieces there. Creates the prefabs in Assets/Prefabs/Level/Street/ if missing.
    /// The layout and every jump stay the same. Does nothing if the section is already dressed.
    ///
    /// Sizes come from the team's mockup (the hero is 1.8 units): hydrant 1.23 tall, dumpster 1.9,
    /// power box 2.9, sidewalk slab 0.93 thick with the walking line 42% down it. Each sprite's pivot
    /// is on its walking line or base, so pieces are placed by where you stand on them.
    /// </summary>
    public static class StreetDressing
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string ArtFolder = "Assets/Art/Street/";
        const string PrefabFolder = "Assets/Prefabs/Level/Street";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";
        static readonly string[] SidewalkVariants = { "Ground1", "Ground2", "Ground3", "Ground4" };

        // How each image is imported and what part of it is solid. Pixel numbers were measured on
        // the drawings: pixelsPerUnit sets the size, pivot is the walking line or base (0-1 from
        // the bottom-left), and collider is width x height in world units with its top at the pivot
        // (for floating chunks) or its bottom at the pivot (for things standing on the ground).
        struct Piece
        {
            public string Prefab, Art;
            public float PixelsPerUnit;
            public Vector2 Pivot;
            public Vector2 Collider;
            public bool StandsOnGround;
            public int SortingOrder;
            public int MaxSize;
        }

        static readonly Piece[] Pieces =
        {
            new Piece { Prefab = "Sidewalk", Art = "Ground1", PixelsPerUnit = 561f, Pivot = new Vector2(0.5f, 0.616f), SortingOrder = 1, MaxSize = 512 },
            new Piece { Prefab = "Road", Art = "Street", PixelsPerUnit = 704f, Pivot = new Vector2(0.5f, 1f), SortingOrder = 1, MaxSize = 1024 },
            new Piece { Prefab = "SidewalkChunk_Pipe", Art = "Tile1", PixelsPerUnit = 600f, Pivot = new Vector2(0.551f, 0.754f), Collider = new Vector2(1.13f, 0.4f), SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "SidewalkChunk_Small", Art = "Tile1_2", PixelsPerUnit = 600f, Pivot = new Vector2(0.551f, 0.754f), Collider = new Vector2(1.13f, 0.4f), SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "SidewalkChunk_Double", Art = "Tile2", PixelsPerUnit = 600f, Pivot = new Vector2(0.53f, 0.74f), Collider = new Vector2(2.12f, 0.4f), SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "SidewalkChunk_Wide", Art = "Tile3", PixelsPerUnit = 453f, Pivot = new Vector2(0.525f, 0.805f), Collider = new Vector2(4.15f, 0.4f), SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "FireHydrant", Art = "FireHydrant", PixelsPerUnit = 656f, Pivot = new Vector2(0.508f, 0.121f), Collider = new Vector2(0.6f, 1.2f), StandsOnGround = true, SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "PowerBox", Art = "PowerBox", PixelsPerUnit = 587f, Pivot = new Vector2(0.494f, 0.03f), Collider = new Vector2(2.4f, 2.78f), StandsOnGround = true, SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "Dumpster", Art = "GarbageBin", PixelsPerUnit = 700f, Pivot = new Vector2(0.502f, 0.038f), Collider = new Vector2(2.2f, 1.85f), StandsOnGround = true, SortingOrder = 2, MaxSize = 512 },
        };

        const float SidewalkSpacing = 1844f / 561f; // the slabs' top-edge width: they tile seamlessly at this spacing
        const float RoadWidth = 4717f / 704f;
        const float PowerBoxHeight = 2.78f;

        [MenuItem("Tools/NoRegRats/Dress Level1 Street")]
        public static void Run()
        {
            EnsureFolder(PrefabFolder);
            foreach (Piece piece in Pieces)
            {
                Sprite sprite = ConfigureSprite(piece.Art, piece);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(piece.Prefab)) == null)
                    CreatePrefab(piece, sprite);
            }
            foreach (string variant in SidewalkVariants)
                ConfigureSprite(variant, Pieces[0]);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            Transform section = GameObject.Find("Level/Subway").transform;
            if (section.Find("Sidewalk") != null)
            {
                Debug.Log("StreetDressing: Level1's street is already dressed.");
                return;
            }

            // Stand-in pieces this replaces.
            foreach (string old in new[] { "TrainCar", "StoneBridge", "BrickLedge", "ScaffoldBar", "Crate", "Crate Stopper", "Crate Wall" })
                for (Transform child = section.Find(old); child != null; child = section.Find(old))
                    Object.DestroyImmediate(child.gameObject);

            // Sidewalk and road along the floor (x -6 to the shaft at 62), ending at the shaft's edge.
            const int slabs = 20;
            float sidewalkStart = 62f - slabs * SidewalkSpacing;
            for (int i = 0; i < slabs; i++)
            {
                GameObject slab = Place("Sidewalk", section, sidewalkStart + SidewalkSpacing * (i + 0.5f), 0f);
                Sprite variant = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + SidewalkVariants[i % SidewalkVariants.Length] + ".png");
                slab.GetComponent<SpriteRenderer>().sprite = variant;
            }
            const int roads = 10;
            float roadStart = 62f - roads * RoadWidth;
            for (int i = 0; i < roads; i++)
                Place("Road", section, roadStart + RoadWidth * (i + 0.5f), -0.52f);

            Place("SidewalkChunk_Small", section, 6f, 1.4f);
            Place("PowerBox", section, 12f, 0f);
            Place("FireHydrant", section, 16.5f, 0f);
            Place("SidewalkChunk_Wide", section, 21f, 2.9f);
            Place("SidewalkChunk_Double", section, 27.5f, 5f);

            // The crate puzzle: the hydrant stops the dumpster from being pushed too far back, the
            // big power box's top (4.2) is out of jumping reach from the floor but not from the
            // dumpster pushed against it.
            Place("FireHydrant", section, 31.3f, 0f).name = "Dumpster Stopper";
            Place("Dumpster", section, 34.6f, 0f);
            GameObject wall = Place("PowerBox", section, 41.9f, 0f);
            wall.name = "Wall Power Box";
            wall.transform.localScale = Vector3.one * (4.2f / PowerBoxHeight);

            Place("FireHydrant", section, 46f, 0f);
            Place("SidewalkChunk_Pipe", section, 54f, 3f);

            // The Plain rat that rode the first train car now patrols the power box's top.
            foreach (RatEnemy rat in section.GetComponentsInChildren<RatEnemy>())
            {
                if (rat.transform.position.y < 1f)
                    continue;
                rat.transform.position = new Vector3(12f, PowerBoxHeight + 0.02f, 0f);
                var serialized = new SerializedObject(rat);
                serialized.FindProperty("patrolRange").floatValue = 0.7f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("StreetDressing: done.");
        }

        static string PrefabPath(string name) => PrefabFolder + "/" + name + ".prefab";

        static GameObject Place(string prefabName, Transform parent, float x, float y)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefabName));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = prefabName;
            instance.transform.position = new Vector3(x, y, 0f);
            return instance;
        }

        static void CreatePrefab(Piece piece, Sprite sprite)
        {
            var go = new GameObject(piece.Prefab);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = piece.SortingOrder;
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteLitMaterialGuid));
            if (material != null)
                renderer.sharedMaterial = material;

            if (piece.Collider != Vector2.zero)
            {
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = piece.Collider;
                collider.offset = new Vector2(0f, piece.StandsOnGround ? piece.Collider.y * 0.5f : -piece.Collider.y * 0.5f);
            }
            if (piece.Prefab == "Dumpster")
            {
                var box = go.AddComponent<PushableBox>();
                box.ApplyDefaultBodySettings();
                var serialized = new SerializedObject(box);
                serialized.FindProperty("pushSpeed").floatValue = 2f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(piece.Prefab));
            Object.DestroyImmediate(go);
        }

        static Sprite ConfigureSprite(string art, Piece piece)
        {
            string path = ArtFolder + art + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = piece.PixelsPerUnit;
            importer.maxTextureSize = piece.MaxSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = piece.Pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
