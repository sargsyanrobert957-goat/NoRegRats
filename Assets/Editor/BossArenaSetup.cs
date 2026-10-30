using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Adds the boss fight to Level1's arena: sizes the boss-fight sprites, creates the RatKing,
    /// Trash, Heart and Gate prefabs if missing, and in the scene places the Rat King, the entrance
    /// gate, an exit hole with its grate and win trigger in the right wall, the arena trigger, and a
    /// HUD canvas with the boss health bar. Only adds what isn't there yet, so it's safe to run again.
    /// </summary>
    public static class BossArenaSetup
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string KingPrefabPath = "Assets/Prefabs/Enemies/RatKing.prefab";
        const string TrashPrefabPath = "Assets/Prefabs/Combat/Trash.prefab";
        const string HeartPrefabPath = "Assets/Prefabs/Items/Heart.prefab";
        const string GatePrefabPath = "Assets/Prefabs/Level/Gate.prefab";
        const string NoFrictionPath = "Assets/Physics/NoFriction.physicsMaterial2D";
        const string SpriteLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        static readonly Color Ink = new Color(0.17f, 0.17f, 0.2f);

        [MenuItem("Tools/NoRegRats/Set Up Boss Arena")]
        public static void Run()
        {
            Sprite kingSprite = ConfigureSprite("Assets/Art/Rats/RatKing.png", byHeight: true, 3f, new Vector2(0.5f, 0f));
            Sprite trashSprite = ConfigureSprite("Assets/Art/Placeholder/Trash.png", byHeight: true, 0.6f, new Vector2(0.5f, 0.5f));
            Sprite heartSprite = ConfigureSprite("Assets/Art/Items/Heart.png", byHeight: false, 0.9f, new Vector2(0.5f, 0.5f));
            Sprite barsSprite = ConfigureBars("Assets/Art/Placeholder/Bars.png");

            EnsureFolder("Assets/Prefabs/Items");
            var trash = AssetDatabase.LoadAssetAtPath<TrashProjectile>(TrashPrefabPath) ?? CreateTrash(trashSprite);
            var heart = AssetDatabase.LoadAssetAtPath<HeartPickup>(HeartPrefabPath) ?? CreateHeart(heartSprite);
            var kingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KingPrefabPath) ?? CreateKing(kingSprite, trash, heart);
            var gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GatePrefabPath) ?? CreateGate(barsSprite);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            Transform arena = GameObject.Find("Level/Arena").transform;
            if (arena.Find("Boss Arena") != null)
            {
                Debug.Log("BossArenaSetup: the arena is already set up.");
                return;
            }

            var king = (GameObject)PrefabUtility.InstantiatePrefab(kingPrefab, arena);
            king.name = "Rat King";
            king.transform.position = arena.Find("Rat King Spot").position;

            SlidingGate entrance = PlaceGate(gatePrefab, arena, "Entrance Gate", new Vector2(112.5f, -15f), 10f, new Vector2(0f, 10f), startOpen: true);

            // Open a 4-high hole at the bottom of the arena's right wall, closed by the exit grate,
            // with the win trigger behind it.
            Transform rightWall = arena.Find("Right Wall");
            rightWall.position = new Vector3(135f, -13f, 0f);
            rightWall.localScale = new Vector3(2f, 6f, 1f);
            SlidingGate exitGrate = PlaceGate(gatePrefab, arena, "Exit Grate", new Vector2(134.5f, -18f), 4f, new Vector2(0f, 5f), startOpen: false);
            var exit = new GameObject("Level Exit");
            exit.transform.SetParent(arena, false);
            exit.transform.position = new Vector3(135.5f, -18f, 0f);
            var exitTrigger = exit.AddComponent<BoxCollider2D>();
            exitTrigger.isTrigger = true;
            exitTrigger.size = new Vector2(1f, 4f);
            exit.AddComponent<LevelExit>();

            var arenaObject = new GameObject("Boss Arena");
            arenaObject.transform.SetParent(arena, false);
            arenaObject.transform.position = new Vector3(124f, -16f, 0f);
            var arenaTrigger = arenaObject.AddComponent<BoxCollider2D>();
            arenaTrigger.isTrigger = true;
            arenaTrigger.size = new Vector2(20f, 8f);
            var cameraSpot = new GameObject("Camera Spot").transform;
            cameraSpot.SetParent(arenaObject.transform, false);
            cameraSpot.position = new Vector3(123f, -14.5f, 0f);

            BossHealthBar bar = CreateHud();
            var bossArena = arenaObject.AddComponent<BossArena>();
            var serialized = new SerializedObject(bossArena);
            serialized.FindProperty("king").objectReferenceValue = king.GetComponent<RatKing>();
            serialized.FindProperty("entranceGate").objectReferenceValue = entrance;
            serialized.FindProperty("exitGrate").objectReferenceValue = exitGrate;
            serialized.FindProperty("cameraFollow").objectReferenceValue = Object.FindAnyObjectByType<CameraFollow2D>();
            serialized.FindProperty("cameraSpot").objectReferenceValue = cameraSpot;
            serialized.FindProperty("healthBar").objectReferenceValue = bar;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("BossArenaSetup: done.");
        }

        static SlidingGate PlaceGate(GameObject prefab, Transform parent, string name, Vector2 closedCenter, float height, Vector2 openOffset, bool startOpen)
        {
            var gate = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            gate.name = name;
            gate.transform.position = closedCenter;
            gate.GetComponent<SpriteRenderer>().size = new Vector2(1f, height);
            gate.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, height);
            var slider = gate.GetComponent<SlidingGate>();
            var serialized = new SerializedObject(slider);
            serialized.FindProperty("openOffset").vector2Value = openOffset;
            serialized.FindProperty("startOpen").boolValue = startOpen;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return slider;
        }

        // A screen-space canvas for the HUD, with the boss bar at the top centre.
        static BossHealthBar CreateHud()
        {
            var hud = new GameObject("HUD", typeof(RectTransform));
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = UiRect("Boss Bar", hud.transform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(820f, 90f));
            RectTransform back = UiRect("Back", panel, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(820f, 44f));
            back.pivot = new Vector2(0.5f, 0f);
            back.gameObject.AddComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.85f);
            // The fill's right anchor tracks the health, inside a padded area of the dark back.
            RectTransform fillArea = Stretch("Fill Area", back, padding: 6f);
            RectTransform fill = Stretch("Fill", fillArea, padding: 0f);
            fill.gameObject.AddComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f);

            RectTransform labelRect = UiRect("Label", panel, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(820f, 44f));
            labelRect.pivot = new Vector2(0.5f, 1f);
            var label = labelRect.gameObject.AddComponent<Text>();
            label.text = "THE RAT KING";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 34;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Ink;

            var bar = hud.AddComponent<BossHealthBar>();
            var serialized = new SerializedObject(bar);
            serialized.FindProperty("panel").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("fill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        // A rect filling its parent, inset by padding on every side.
        static RectTransform Stretch(string name, Transform parent, float padding)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        static RectTransform UiRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // --- Prefabs ---

        static GameObject CreateKing(Sprite sprite, TrashProjectile trash, HeartPickup heart)
        {
            var root = new GameObject("RatKing");
            var body = root.AddComponent<Rigidbody2D>();
            body.mass = 20f;
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(1.6f, 2.6f);
            collider.offset = new Vector2(0f, 1.3f);
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
            ApplySpriteMaterial(renderer);

            Set(root.AddComponent<Health>(), "maxHealth", 8);
            var king = root.AddComponent<RatKing>();
            Set(king, "visual", visual.transform);
            Set(king, "trashPrefab", trash);
            Set(king, "heartPrefab", heart);
            Set(root.AddComponent<ContactDamage>(), "damage", 2);
            Set(root.AddComponent<DamageFlash>(), "target", renderer);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, KingPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static TrashProjectile CreateTrash(Sprite sprite)
        {
            var go = new GameObject("Trash");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 15;
            ApplySpriteMaterial(renderer);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 1.5f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.22f;
            go.AddComponent<TrashProjectile>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, TrashPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<TrashProjectile>();
        }

        static HeartPickup CreateHeart(Sprite sprite)
        {
            var go = new GameObject("Heart");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;
            ApplySpriteMaterial(renderer);
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            go.AddComponent<HeartPickup>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, HeartPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<HeartPickup>();
        }

        static GameObject CreateGate(Sprite sprite)
        {
            var go = new GameObject("Gate");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(1f, 4f);
            renderer.sortingOrder = -1;
            ApplySpriteMaterial(renderer);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 4f);
            go.AddComponent<SlidingGate>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, GatePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // --- Sprites ---

        static Sprite ConfigureSprite(string path, bool byHeight, float worldSize, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = (byHeight ? height : width) / worldSize;
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

        // The bars tile vertically: 128 px per unit, full-rect mesh, repeating texture.
        static Sprite ConfigureBars(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
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

        static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            switch (value)
            {
                case int i: property.intValue = i; break;
                case float f: property.floatValue = f; break;
                case Object o: property.objectReferenceValue = o; break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
