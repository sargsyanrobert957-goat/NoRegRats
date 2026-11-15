using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Builds Level1's menus from the team's drawings in Assets/Art/UI/Menus/: the title screen
    /// (Start, Continue, Exit and the hero leaning in from the right), the pause screen (Continue,
    /// Restart, Quit) with the controls sheet beside it, You Won (with the run's stats written next
    /// to the drawn labels, Play Again, Quit) and You Lost (Restart, Quit). Also adds the lives
    /// counter under the hearts, the pause button in the top-right corner and the Click sound to
    /// the scene's sound list. Replaces the old plain menus if the drawn ones aren't there yet;
    /// adds each of the other pieces only if it's missing.
    ///
    /// Every drawing sits where it does in the artist's mockups: positions below are each PNG's
    /// top-left corner in pixels on its mockup (the 2160 x 1620 title board, or the 1613 x 1456
    /// window drawing), and the PNGs are used at the mockups' scale.
    /// </summary>
    public static class MenuSetup
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string ArtFolder = "Assets/Art/UI/Menus/";
        const string FontFolder = "Assets/Art/Fonts";

        // The HUD scales from 1920 x 1080. The title board is shown at the screen's height; the
        // windows a bit smaller, and smaller still on the pause screen, which has two side by side.
        const float ReferenceHeight = 1080f;
        static readonly Vector2 TitleBoard = new Vector2(2160f, 1620f);
        static readonly Vector2 Window = new Vector2(1613f, 1456f);
        const float WindowScale = 0.6f;
        const float PauseWindowScale = 0.55f;
        const float PauseWindowGap = 40f;
        const float ControlsInWindow = 0.85f;

        static readonly Color TitleBackground = new Color32(187, 187, 187, 255);
        static readonly Color Dim = new Color(0.17f, 0.17f, 0.2f, 0.6f);
        static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);
        static readonly Color HudInk = new Color(0.17f, 0.17f, 0.2f);
        static readonly Color Paper = new Color(0.98f, 0.97f, 0.94f);

        static Font font;

        [MenuItem("Tools/NoRegRats/Set Up Menus")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            Transform hud = GameObject.Find("HUD").transform;
            if (hud.Find("Lose Screen") == null)
            {
                foreach (string old in new[] { "Title Menu", "Pause Menu", "Win Screen" })
                    for (Transform menu = hud.Find(old); menu != null; menu = hud.Find(old))
                        Object.DestroyImmediate(menu.gameObject);
                var playerHealth = Object.FindAnyObjectByType<PlayerMovement>().GetComponent<Health>();
                Build(hud, playerHealth);
            }
            BuildLives(hud);
            BuildPauseButton(hud);
            AddClickSound();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MenuSetup: done.");
        }

        /// <summary>The title, pause, won and lost screens, drawn over everything else on the HUD.</summary>
        internal static void Build(Transform hud, Health playerHealth)
        {
            font = FindFont();
            BuildWin(hud, playerHealth);
            BuildLose(hud);
            BuildPause(hud);
            BuildTitle(hud);
            foreach (string menu in new[] { "Win Screen", "Lose Screen", "Pause Menu", "Title Menu" })
                hud.Find(menu).SetAsLastSibling();
        }

        /// <summary>"Lives: 3" under the hearts; the "+1 heart!" popup moves below it.</summary>
        internal static void BuildLives(Transform hud)
        {
            if (hud.Find("Lives") != null)
                return;
            font = FindFont();
            Text label = Label("Lives", hud, "Lives: 3", 38, HudInk, TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -112f), new Vector2(400f, 50f), new Vector2(0f, 1f));
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Paper;
            outline.effectDistance = new Vector2(2f, -2f);
            SetObject(label.gameObject.AddComponent<LivesDisplay>(), "label", label);
            // With the HUD, under the menus.
            label.transform.SetSiblingIndex(hud.Find("Hearts").GetSiblingIndex() + 1);

            Transform popup = hud.Find("Hearts/Bonus Popup");
            if (popup != null)
                ((RectTransform)popup).anchoredPosition = new Vector2(0f, -135f);
        }

        /// <summary>A drawn pause button in the top-right corner, opening the pause menu during play.</summary>
        internal static void BuildPauseButton(Transform hud)
        {
            if (hud.Find("Pause Button") != null)
                return;
            Sprite sprite = Art("PauseButton");
            // 40 px in from the right and 25 down, placed by its centre so it grows and tilts about its middle.
            RectTransform rect = Rect("Pause Button", hud, new Vector2(1f, 1f), new Vector2(-95f, -80f), new Vector2(110f, 110f), new Vector2(0.5f, 0.5f));
            Image image = Picture(rect, sprite);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 0.95f, 0.78f);
            colors.pressedColor = new Color(0.9f, 0.84f, 0.68f);
            button.colors = colors;
            // Never selected, so Space (attack, and the UI's submit) can't press it during play.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var feedback = rect.gameObject.AddComponent<MenuButtonFeedback>();
            var serialized = new SerializedObject(feedback);
            serialized.FindProperty("selectOnHover").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // With the HUD, under the menus.
            rect.SetSiblingIndex(hud.Find("Hearts").GetSiblingIndex() + 1);
            SetObject(Get<PauseMenu>(hud), "pauseButton", button);
        }

        // --- Screens ---

        static void BuildTitle(Transform hud)
        {
            GameObject panel = FullScreen("Title Menu", hud, TitleBackground);
            float k = ReferenceHeight / TitleBoard.y;
            RectTransform board = Board("Board", panel.transform, TitleBoard * k, Vector2.zero);
            Piece("Title", board, Art("Title/Title"), new Vector2(148f, 124f), k);
            Button start = ButtonPiece("Start", board, Art("Title/Start"), new Vector2(120f, 379f), k);
            Button resume = ButtonPiece("Continue", board, Art("Title/Continue"), new Vector2(628f, 749f), k);
            Button exit = ButtonPiece("Exit", board, Art("Title/Exit"), new Vector2(233f, 1044f), k);

            // The hero leans in from the right edge of the screen, cut off by it as in the drawing
            // (the PNG runs 37 px past the board's edge), whatever the screen's width.
            Sprite heroSprite = Art("Title/Character");
            RectTransform hero = Rect("Character", panel.transform, new Vector2(1f, 0f), new Vector2(37f * k, 0f), heroSprite.rect.size * k, new Vector2(1f, 0f));
            Picture(hero, heroSprite);

            var menu = Get<TitleMenu>(hud);
            SetObject(menu, "panel", panel);
            SetObject(menu, "startButton", start);
            SetObject(menu, "continueButton", resume);
            SetObject(menu, "exitButton", exit);
        }

        static void BuildPause(Transform hud)
        {
            GameObject panel = FullScreen("Pause Menu", hud, Dim);
            float s = PauseWindowScale;
            float offset = (Window.x * s + PauseWindowGap) * 0.5f;

            RectTransform window = Board("Window", panel.transform, Window * s, new Vector2(-offset, 0f));
            Piece("Back", window, Art("Window"), Vector2.zero, s);
            Button resume = ButtonPiece("Continue", window, Art("Paused/Continue"), new Vector2(411f, 185f), s);
            Button restart = ButtonPiece("Restart", window, Art("Restart"), new Vector2(419f, 507f), s);
            Button quit = ButtonPiece("Quit", window, Art("Quit"), new Vector2(382f, 869f), s);

            RectTransform controls = Board("Controls", panel.transform, Window * s, new Vector2(offset, 0f));
            Piece("Back", controls, Art("Window"), Vector2.zero, s);
            Sprite keys = Art("Controls");
            float keysScale = s * ControlsInWindow;
            Picture(Rect("Keys", controls, new Vector2(0.5f, 0.5f), Vector2.zero, keys.rect.size * keysScale, new Vector2(0.5f, 0.5f)), keys);

            var menu = Get<PauseMenu>(hud);
            SetObject(menu, "panel", panel);
            SetObject(menu, "continueButton", resume);
            SetObject(menu, "restartButton", restart);
            SetObject(menu, "quitButton", quit);
        }

        static void BuildWin(Transform hud, Health playerHealth)
        {
            GameObject panel = FullScreen("Win Screen", hud, Dim);
            float s = WindowScale;
            RectTransform window = Board("Window", panel.transform, Window * s, Vector2.zero);
            Piece("Back", window, Art("Window"), Vector2.zero, s);
            Piece("Title", window, Art("YouWon/YouWon"), new Vector2(224f, 140f), s);
            Piece("Stats", window, Art("YouWon/Stats"), new Vector2(284f, 446f), s);
            // Each value starts just right of its label's colon, on the label's middle line.
            Text time = Value("Time", window, new Vector2(840f, 590f), s);
            Text deaths = Value("Deaths", window, new Vector2(835f, 692f), s);
            Text hearts = Value("Hearts Left", window, new Vector2(1000f, 802f), s);
            Button again = ButtonPiece("Play Again", window, Art("YouWon/PlayAgain"), new Vector2(538f, 895f), s);
            Button quit = ButtonPiece("Quit", window, Art("YouWon/Quit"), new Vector2(503f, 1047f), s);

            var screen = Get<WinScreen>(hud);
            SetObject(screen, "panel", panel);
            SetObject(screen, "timeValue", time);
            SetObject(screen, "deathsValue", deaths);
            SetObject(screen, "heartsValue", hearts);
            SetObject(screen, "playAgainButton", again);
            SetObject(screen, "quitButton", quit);
            SetObject(screen, "playerHealth", playerHealth);
        }

        static void BuildLose(Transform hud)
        {
            GameObject panel = FullScreen("Lose Screen", hud, Dim);
            float s = WindowScale;
            RectTransform window = Board("Window", panel.transform, Window * s, Vector2.zero);
            Piece("Back", window, Art("Window"), Vector2.zero, s);
            Piece("Title", window, Art("YouLost/YouLost"), new Vector2(359f, 196f), s);
            Button restart = ButtonPiece("Restart", window, Art("Restart"), new Vector2(408f, 501f), s);
            Button quit = ButtonPiece("Quit", window, Art("Quit"), new Vector2(386f, 842f), s);

            var screen = Get<LoseScreen>(hud);
            SetObject(screen, "panel", panel);
            SetObject(screen, "restartButton", restart);
            SetObject(screen, "quitButton", quit);
        }

        static void AddClickSound()
        {
            var sfx = Object.FindAnyObjectByType<Sfx>();
            if (sfx == null)
                return;
            var serialized = new SerializedObject(sfx);
            SerializedProperty sounds = serialized.FindProperty("sounds");
            for (int i = 0; i < sounds.arraySize; i++)
                if (sounds.GetArrayElementAtIndex(i).FindPropertyRelative("sound").intValue == (int)Sfx.Sound.Click)
                    return;
            sounds.arraySize++;
            SerializedProperty entry = sounds.GetArrayElementAtIndex(sounds.arraySize - 1);
            entry.FindPropertyRelative("sound").intValue = (int)Sfx.Sound.Click;
            entry.FindPropertyRelative("clips").arraySize = 0;
            entry.FindPropertyRelative("volume").floatValue = 0.6f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Pieces ---

        // A full-screen panel that also blocks clicks to anything behind it.
        static GameObject FullScreen(string name, Transform hud, Color color)
        {
            RectTransform rect = Rect(name, hud, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.gameObject.AddComponent<Image>().color = color;
            return rect.gameObject;
        }

        // An area the size of a mockup (already scaled), centred on the screen plus an offset.
        static RectTransform Board(string name, Transform parent, Vector2 size, Vector2 offset) =>
            Rect(name, parent, new Vector2(0.5f, 0.5f), offset, size, new Vector2(0.5f, 0.5f));

        // A drawing placed by its top-left corner on the board, in mockup pixels. Pivoted at its
        // centre so buttons grow and tilt about their middle.
        static Image Piece(string name, RectTransform board, Sprite sprite, Vector2 topLeft, float scale)
        {
            Vector2 size = sprite.rect.size * scale;
            var position = new Vector2(topLeft.x * scale + size.x * 0.5f, -(topLeft.y * scale + size.y * 0.5f));
            return Picture(Rect(name, board, new Vector2(0f, 1f), position, size, new Vector2(0.5f, 0.5f)), sprite);
        }

        static Image Picture(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static Button ButtonPiece(string name, RectTransform board, Sprite sprite, Vector2 topLeft, float scale)
        {
            Image image = Piece(name, board, sprite, topLeft, scale);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 0.95f, 0.78f);
            colors.pressedColor = new Color(0.9f, 0.84f, 0.68f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
            // Up / down only, skipping buttons that can't be used (Continue with no run).
            button.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            image.gameObject.AddComponent<MenuButtonFeedback>();
            return button;
        }

        // A stat written in the window: left-aligned, centred on a mockup line, sized to the
        // drawn labels' letters.
        static Text Value(string name, RectTransform window, Vector2 leftMiddle, float scale)
        {
            Text label = Label(name, window, "", Mathf.RoundToInt(80f * scale), Ink, TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(leftMiddle.x * scale, -leftMiddle.y * scale),
                new Vector2((Window.x - leftMiddle.x - 100f) * scale, 110f * scale), new Vector2(0f, 0.5f));
            return label;
        }

        static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor alignment)
        {
            RectTransform rect = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 60f), new Vector2(0.5f, 0.5f));
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, anchor, position, size, pivot);
            return rect;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static T Get<T>(Transform hud) where T : Component =>
            hud.TryGetComponent(out T component) ? component : hud.gameObject.AddComponent<T>();

        static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Font FindFont()
        {
            if (AssetDatabase.IsValidFolder(FontFolder))
                foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { FontFolder }))
                    return AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // Menu drawings are shown big, so they keep up to 2048 px (Assets/Art's default is 512).
        static Sprite Art(string name)
        {
            string path = ArtFolder + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
