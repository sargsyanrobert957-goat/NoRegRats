using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Tools > NoRegRats > Build Start Wall: the brick wall at the very start of the street with the
    /// controls written on it in black (the player's own keys in the hand-drawn capitals, then the
    /// artist's arrow and action words), in place of the old controls card in the corner. Builds the
    /// wall in Level1 under Level/Subway (the street) if it isn't there, removes the corner card,
    /// gives the menus' key boxes the drawn capitals, and moves the first checkpoint sign just past
    /// the hero's start. Safe to run again.
    /// </summary>
    public static class StartWallSetup
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string Folder = "Assets/Art/Level/StartWall/";
        // The wall picture is drawn at 160 pixels per world unit (5 x 4.6 units).
        const float WallPixelsPerUnit = 160f;
        // Its bottom-left corner: just right of the level's dark left edge, the bottom behind the
        // sidewalk. It ends at x 1.1, before the hero's start (x 2).
        static readonly Vector2 WallSpot = new Vector2(-3.9f, -0.25f);
        // The first checkpoint's sign moves from x 0.8 to just past the hero's start (x 2), clear of
        // the wall's writing; its board is above the hero's head.
        const float FirstCheckpointX = 2.7f;

        // The paint, laid out in canvas units (100 to a world unit) from the wall's bottom-left.
        const float CanvasScale = 0.01f;
        const float CapHeight = 46f;
        const float KeysRight = 215f;
        const float ArrowLeft = 235f;
        const float ArrowHeight = 23f;
        const float WordGap = 20f;
        // The words are drawn with capitals about 200 px tall; scaled so theirs match the keys'.
        const float WordScale = CapHeight * 1.25f / 200f;
        // Rows from the top; the bottom one stays clear of the sidewalk in front.
        const float FirstRowY = 371f;
        const float RowStep = 68f;
        static readonly float[] RowTilt = { -1.5f, 1f, -0.6f, 1.2f, -1f };
        // Black, like the ink of the controls sheet.
        static readonly Color Paint = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        static readonly (KeyBindings.Control[] keys, string word)[] Rows =
        {
            (new[] { KeyBindings.Control.RunLeft, KeyBindings.Control.RunRight }, "Run"),
            (new[] { KeyBindings.Control.Jump }, "Jump"),
            (new[] { KeyBindings.Control.Crouch }, "Crouch"),
            (new[] { KeyBindings.Control.Punch }, "Punch"),
            (new[] { KeyBindings.Control.Pause }, "Pause"),
        };

        [MenuItem("Tools/NoRegRats/Build Start Wall")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            // After opening the scene: opening one unloads assets nothing is holding yet.
            DrawnAlphabet alphabet = AlphabetSetup.Get();

            Transform street = GameObject.Find("Level").transform.Find("Subway");
            if (street.Find("Start Wall") == null)
                BuildWall(street, alphabet);
            UsePaintColour(street.Find("Start Wall"));

            Transform hud = GameObject.Find("HUD").transform;
            MenuSetup.RemoveControlsHint(hud);
            MenuSetup.UseDrawnKeyNames(hud);

            Transform first = GameObject.Find("Level").transform.Find("Checkpoints/Checkpoint 0");
            if (first != null && first.position.x < FirstCheckpointX)
                first.position = new Vector3(FirstCheckpointX, first.position.y, first.position.z);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("StartWallSetup: done.");
        }

        static void BuildWall(Transform street, DrawnAlphabet alphabet)
        {
            var wall = new GameObject("Start Wall");
            wall.transform.SetParent(street, false);
            wall.transform.position = WallSpot;
            var bricks = wall.AddComponent<SpriteRenderer>();
            bricks.sprite = Import("StartWall", WallPixelsPerUnit, SpriteAlignment.BottomLeft, 2048);
            // In front of the city, behind the sidewalk (1) and everything on it.
            bricks.sortingOrder = -1;

            // The paint: a world-space canvas over the wall.
            var paint = new GameObject("Paint", typeof(RectTransform));
            var area = (RectTransform)paint.transform;
            area.SetParent(wall.transform, false);
            area.pivot = Vector2.zero;
            area.sizeDelta = bricks.sprite.rect.size / WallPixelsPerUnit / CanvasScale;
            area.localPosition = Vector3.zero;
            area.localScale = Vector3.one * CanvasScale;
            var canvas = paint.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            var controlsWall = paint.AddComponent<ControlsWall>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite arrowArt = Import("Arrow", 100f, SpriteAlignment.Center, 1024);

            var so = new SerializedObject(controlsWall);
            SerializedProperty rows = so.FindProperty("rows");
            rows.arraySize = Rows.Length;
            for (int i = 0; i < Rows.Length; i++)
            {
                (KeyBindings.Control[] keys, string word) = Rows[i];
                // A row, tilted a little as if painted by hand.
                RectTransform row = Rect("Row " + (i + 1), area, new Vector2(0f, 0f), new Vector2(0f, FirstRowY - i * RowStep), new Vector2(area.sizeDelta.x, RowStep), new Vector2(0f, 0.5f));
                row.localRotation = Quaternion.Euler(0f, 0f, RowTilt[i % RowTilt.Length]);

                RectTransform keysRect = Rect("Keys", row, new Vector2(0f, 0.5f), new Vector2(KeysRight, 0f), new Vector2(KeysRight - 10f, 70f), new Vector2(1f, 0.5f));
                var fallback = keysRect.gameObject.AddComponent<Text>();
                fallback.font = font;
                fallback.fontSize = 44;
                fallback.fontStyle = FontStyle.Bold;
                fallback.color = Paint;
                fallback.alignment = TextAnchor.MiddleRight;
                fallback.horizontalOverflow = HorizontalWrapMode.Overflow;
                fallback.raycastTarget = false;
                fallback.enabled = false;
                var drawn = keysRect.gameObject.AddComponent<DrawnText>();
                var drawnSo = new SerializedObject(drawn);
                drawnSo.FindProperty("alphabet").objectReferenceValue = alphabet;
                drawnSo.FindProperty("capHeight").floatValue = CapHeight;
                drawnSo.FindProperty("color").colorValue = Paint;
                drawnSo.FindProperty("alignment").enumValueIndex = (int)DrawnText.Align.Right;
                drawnSo.FindProperty("fallback").objectReferenceValue = fallback;
                drawnSo.ApplyModifiedPropertiesWithoutUndo();

                Vector2 arrowSize = arrowArt.rect.size * (ArrowHeight / arrowArt.rect.height);
                Picture(Rect("Arrow", row, new Vector2(0f, 0.5f), new Vector2(ArrowLeft, 0f), arrowSize, new Vector2(0f, 0.5f)), arrowArt);
                Sprite wordArt = Import(word, 100f, SpriteAlignment.Center, 1024);
                Vector2 wordSize = wordArt.rect.size * WordScale;
                // A little high, as the words' tails (J, P) hang below the line.
                Picture(Rect(word, row, new Vector2(0f, 0.5f), new Vector2(ArrowLeft + arrowSize.x + WordGap, wordSize.y * 0.05f), wordSize, new Vector2(0f, 0.5f)), wordArt);

                SerializedProperty entry = rows.GetArrayElementAtIndex(i);
                SerializedProperty controls = entry.FindPropertyRelative("controls");
                controls.arraySize = keys.Length;
                for (int k = 0; k < keys.Length; k++)
                    controls.GetArrayElementAtIndex(k).enumValueIndex = (int)keys[k];
                entry.FindPropertyRelative("keys").objectReferenceValue = drawn;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Keeps a wall built earlier (in white) in the current colour: the keys, the arrows, the
        // words and the plain-font fallback.
        static void UsePaintColour(Transform wall)
        {
            foreach (DrawnText keys in wall.GetComponentsInChildren<DrawnText>(true))
            {
                var so = new SerializedObject(keys);
                so.FindProperty("color").colorValue = Paint;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (Graphic graphic in wall.GetComponentsInChildren<Graphic>(true))
            {
                graphic.color = Paint;
                EditorUtility.SetDirty(graphic);
            }
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void Picture(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Paint;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static Sprite Import(string name, float pixelsPerUnit, SpriteAlignment pivot, int maxSize)
        {
            string path = Folder + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = maxSize;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
