using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Adds the subway station (the team's "Level 2" art in Assets/Art/Subway/) to Level1 between
    /// the street and the sewers, so the route is city → subway → sewers. The shaft, the sewers and
    /// the arena move right by SubwayLength to make room; the street's hole at x 62–66 now drops
    /// into the station (floor top y = -8, ceiling y = -0.5) and the "SEWERS" shaft is at the
    /// station's far end. The station has the tiled platform floor, subway props to hop over or
    /// climb (fire extinguishers, wet-floor signs, recycling bins, a phone booth), floating platform
    /// tiles and one rat of each type. Creates the prefabs in Assets/Prefabs/Level/Subway/ if
    /// missing. Does nothing if the station is already there.
    ///
    /// Sizes come from the artist's mockup (the hero is 1.8 units): extinguisher 1.08 tall, wet-floor
    /// sign 1.17, bins 1.57, phone booth 2.72, one platform tile about 0.85 wide. Each sprite's pivot
    /// is on its walking line or base, so pieces are placed by where you stand.
    /// </summary>
    public static class SubwaySetup
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string ArtFolder = "Assets/Art/Subway/";
        const string PrefabFolder = "Assets/Prefabs/Level/Subway";
        const string LevelPrefabFolder = "Assets/Prefabs/Level/";
        const string EnemyPrefabFolder = "Assets/Prefabs/Enemies/";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        /// <summary>How far the shaft, sewers and arena move right.</summary>
        public const float SubwayLength = 56f;
        const float StreetEnd = 62f;
        const float FloorY = -8f;
        const float CeilingY = -0.5f;

        // The platform floor drawing is a slanted strip that tiles at its walking-line width
        // (2646 px). Its lower-left corner reaches 385 px further left, so the first piece starts
        // that far right of the street's edge; at this size eleven pieces fill the station exactly.
        const float PlatformPixelsPerUnit = 526.6f;
        const float PlatformSpacing = 2646f / PlatformPixelsPerUnit;
        const float PlatformLeftOverhang = 385f / PlatformPixelsPerUnit;
        const int PlatformCount = 11;

        // How each image is imported and what part of it is solid, as in StreetDressing: pivot is
        // the walking line or base (0-1 from the bottom-left), collider is width x height in world
        // units with its top at the pivot (floating tiles) or its bottom at the pivot (props).
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
            new Piece { Prefab = "Platform", Art = "Ground", PixelsPerUnit = PlatformPixelsPerUnit, Pivot = new Vector2(0.541f, 0.663f), SortingOrder = 1, MaxSize = 1024 },
            new Piece { Prefab = "PlatformTile_Single", Art = "Tile1", PixelsPerUnit = 780f, Pivot = new Vector2(0.494f, 0.583f), Collider = new Vector2(0.85f, 0.4f), SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "PlatformTile_Double", Art = "Tile2", PixelsPerUnit = 734f, Pivot = new Vector2(0.534f, 0.42f), Collider = new Vector2(1.7f, 0.4f), SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "PlatformTile_Triple", Art = "Tile3", PixelsPerUnit = 729f, Pivot = new Vector2(0.497f, 0.56f), Collider = new Vector2(2.55f, 0.4f), SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "FireExtinguisher", Art = "FireExtinguisher", PixelsPerUnit = 729f, Pivot = new Vector2(0.531f, 0.099f), Collider = new Vector2(0.4f, 1f), StandsOnGround = true, SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "WetFloorSign", Art = "WetFloorSign", PixelsPerUnit = 809f, Pivot = new Vector2(0.508f, 0.105f), Collider = new Vector2(0.7f, 1.1f), StandsOnGround = true, SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "RecycleBins", Art = "RecycleBin", PixelsPerUnit = 815f, Pivot = new Vector2(0.512f, 0.083f), Collider = new Vector2(2.2f, 1.5f), StandsOnGround = true, SortingOrder = 2, MaxSize = 512 },
            new Piece { Prefab = "PhoneBooth", Art = "PhoneBooth", PixelsPerUnit = 712f, Pivot = new Vector2(0.507f, 0.024f), Collider = new Vector2(1.2f, 2.65f), StandsOnGround = true, SortingOrder = 2, MaxSize = 512 },
        };

        [MenuItem("Tools/NoRegRats/Build Subway Station")]
        public static void Run()
        {
            EnsureFolder(PrefabFolder);
            foreach (Piece piece in Pieces)
            {
                Sprite sprite = ConfigureSprite(piece);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(piece.Prefab)) == null)
                    CreatePrefab(piece, sprite);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath);
            Transform level = GameObject.Find("Level").transform;
            if (level.Find("Subway Station") != null)
            {
                Debug.Log("SubwaySetup: the subway station is already there.");
                return;
            }

            MakeRoom(level);
            Transform street = level.Find("Subway");
            var station = new GameObject("Subway Station").transform;
            station.SetParent(level, false);
            station.SetSiblingIndex(street.GetSiblingIndex() + 1);

            // The street's "SEWERS" sign moves to the station's shaft; a "SUBWAY" sign takes its place.
            Transform sewersSign = street.Find("Sign_Sewers");
            Vector3 signSpot = sewersSign.position;
            sewersSign.SetParent(station, true);
            sewersSign.position = new Vector3(signSpot.x + SubwayLength, FloorY + signSpot.y, 0f);
            Instantiate(LevelPrefabFolder + "Sign_Subway", station, "Sign_Subway", signSpot);

            // Solid rock: the floor (also filling under the street's end), and the ceiling, whose
            // left face is the hole's right wall up at street level.
            GameObject block = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabFolder + "Block.prefab");
            Block(block, station, "Floor", 54f, -24f, StreetEnd - 6f + SubwayLength, FloorY);
            Block(block, station, "Floor Over Sewers", StreetEnd - 6f + SubwayLength, -12f, StreetEnd + SubwayLength, FloorY);
            Block(block, station, "Ceiling", 66f, CeilingY, 66f + SubwayLength, 14f);

            for (int i = 0; i < PlatformCount; i++)
                Place("Platform", station, StreetEnd + PlatformLeftOverhang + PlatformSpacing * (i + 0.5f), FloorY);

            // Left to right. Every top is within the hero's 3.5 jump of the floor or the piece
            // before it, and every gap within 4.5 across.
            Place("FireExtinguisher", station, 70f, FloorY);
            Place("WetFloorSign", station, 72.5f, FloorY);
            Rat("Rat_Plain", station, 77f, 2f);
            Place("RecycleBins", station, 82f, FloorY);                   // top -6.5
            Place("PlatformTile_Double", station, 86.5f, -5f);
            Place("PlatformTile_Double", station, 91f, -3f);
            Place("FireExtinguisher", station, 91.55f, -3f);              // at the tile's right end, as in the mockup
            Place("PhoneBooth", station, 96f, FloorY);                    // top -5.35, has to be jumped
            Rat("Rat_RedCap", station, 100f, 2.5f);
            Place("PlatformTile_Single", station, 101f, -4.5f);
            Place("PlatformTile_Triple", station, 106.5f, -5f);
            Rat("Rat_Jacket", station, 109f, 2f);
            Place("WetFloorSign", station, 111.5f, FloorY);
            Place("RecycleBins", station, 114f, FloorY);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SubwaySetup: done.");
        }

        // Moves everything past the street right, and widens the camera's bounds and the paper to match.
        static void MakeRoom(Transform level)
        {
            foreach (string group in new[] { "Shaft", "Sewers", "Arena" })
                foreach (Transform child in level.Find(group))
                    child.position += new Vector3(SubwayLength, 0f, 0f);

            var follow = Object.FindAnyObjectByType<CameraFollow2D>();
            var serialized = new SerializedObject(follow);
            SerializedProperty bounds = serialized.FindProperty("bounds");
            Rect rect = bounds.rectValue;
            rect.width += SubwayLength;
            bounds.rectValue = rect;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // The paper drifts at 0.6 of the camera, so it trails 0.4 of the extra length behind;
            // starting it a fifth of the length further right keeps both ends covered.
            var paper = Object.FindAnyObjectByType<ParallaxBackground>();
            paper.transform.position += new Vector3(SubwayLength * 0.2f, 0f, 0f);
        }

        static void Rat(string prefabName, Transform parent, float x, float patrolRange)
        {
            GameObject rat = Instantiate(EnemyPrefabFolder + prefabName, parent, prefabName, new Vector3(x, FloorY + 0.02f, 0f));
            var serialized = new SerializedObject(rat.GetComponent<RatEnemy>());
            serialized.FindProperty("patrolRange").floatValue = patrolRange;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Block(GameObject prefab, Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var block = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            block.name = name;
            block.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            block.transform.localScale = new Vector3(xMax - xMin, yMax - yMin, 1f);
        }

        static GameObject Place(string prefabName, Transform parent, float x, float y) =>
            Instantiate(PrefabFolder + "/" + prefabName, parent, prefabName, new Vector3(x, y, 0f));

        static GameObject Instantiate(string prefabPathWithoutExtension, Transform parent, string name, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathWithoutExtension + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            return instance;
        }

        static string PrefabPath(string name) => PrefabFolder + "/" + name + ".prefab";

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
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(piece.Prefab));
            Object.DestroyImmediate(go);
        }

        static Sprite ConfigureSprite(Piece piece)
        {
            string path = ArtFolder + piece.Art + ".png";
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
