using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Builds Level1's menus from the team's drawings in Assets/Art/UI/Menus/: the title screen
    /// (Start, Continue, Controls, Exit and the hero leaning in from the right) with its Controls
    /// screen, the pause screen (Continue, Restart, Quit) with the controls window beside it, You Won
    /// (with the run's stats written next to the drawn labels, Play Again, Quit) and You Lost
    /// (Restart, Quit), and the full-screen lives count. Also makes the hearts in the corner bigger
    /// (and removes the old "Lives: 3" label), and adds the pause button in the top-right corner,
    /// the controls hint and the Click sound to the scene's sound list. Replaces
    /// the old plain menus if the drawn ones aren't there yet, and the old fixed picture of the
    /// controls with the changeable keys; adds each of the other pieces only if it's missing.
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
        const float ControlsHintScale = 0.28f;

        // The controls window, in pixels on the window drawing: one row per control, its key box(es)
        // ending at KeyRight, then the drawn arrow and word (cut from the artist's controls sheet,
        // Controls.png, at WordScale of its size). Reset sits under the rows.
        static readonly (KeyBindings.Control[] keys, string word)[] ControlRows =
        {
            (new[] { KeyBindings.Control.RunLeft, KeyBindings.Control.RunRight }, "Run"),
            (new[] { KeyBindings.Control.Jump }, "Jump"),
            (new[] { KeyBindings.Control.Crouch }, "Crouch"),
            (new[] { KeyBindings.Control.Punch }, "Punch"),
            (new[] { KeyBindings.Control.Pause }, "Pause"),
        };
        const float RowTop = 185f;
        const float RowStep = 205f;
        const float KeyRight = 720f;
        const float KeyHeight = 170f;
        const float KeyGap = 30f;
        const float ArrowLeft = 760f;
        const float WordGap = 40f;
        const float WordScale = 0.62f;
        const float PromptY = 1115f;
        const float ResetY = 1278f;
        const float SmallButtonScale = 0.42f;
        // Key.png's frame, kept unstretched when a key box widens (left, bottom, right, top).
        static readonly Vector4 KeyFrame = new Vector4(120f, 97f, 116f, 140f);

        // The hearts in the corner, times their first size.
        const float HeartsScale = 1.6f;
        // The lives screen, in canvas units (1080 high).
        const float LivesHeroHeight = 520f;
        const float LivesNumberHeight = 330f;
        const float LivesTimesScale = 0.9f;

        static readonly Color TitleBackground = new Color32(187, 187, 187, 255);
        static readonly Color Dim = new Color(0.17f, 0.17f, 0.2f, 0.6f);
        static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f);

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
            HeartsOnly(hud);
            BuildLivesScreen(hud);
            BuildPauseButton(hud);
            BuildControlsHint(hud);
            BuildKeyControls(hud);
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

        /// <summary>
        /// Hearts only in the top-left corner, HeartsScale times their old size: the old "Lives: 3"
        /// label is removed (the lives screen shows the count), and the "+1 heart!" popup sits
        /// right under the bigger hearts at about its old text size.
        /// </summary>
        internal static void HeartsOnly(Transform hud)
        {
            Transform oldLabel = hud.Find("Lives");
            if (oldLabel != null)
                Object.DestroyImmediate(oldLabel.gameObject);
            Transform hearts = hud.Find("Hearts");
            hearts.localScale = Vector3.one * HeartsScale;
            var popup = (RectTransform)hearts.Find("Bonus Popup");
            if (popup != null)
            {
                popup.anchoredPosition = new Vector2(0f, -85f);
                popup.localScale = Vector3.one * (1.2f / HeartsScale);
            }
        }

        /// <summary>
        /// The full-screen lives count (LivesScreen): the hero, "×" and the number, drawn from the
        /// artist's strokes, on the title's grey. Above the HUD and the other screens, under the title.
        /// </summary>
        internal static void BuildLivesScreen(Transform hud)
        {
            if (hud.Find("Lives Screen") != null)
                return;
            GameObject panel = FullScreen("Lives Screen", hud, TitleBackground);
            panel.AddComponent<CanvasGroup>();

            Sprite heroArt = Art("Lives/Hero");
            Sprite timesArt = Art("Lives/Times");
            Sprite strikeArt = Art("Lives/Strike");
            Sprite[] numbers = { Art("Lives/One"), Art("Lives/Two"), Art("Lives/Three") };

            // Hero, gap, ×, gap, number box: laid out left to right, centred as a group.
            Vector2 hero = heroArt.rect.size * (LivesHeroHeight / heroArt.rect.height);
            Vector2 times = timesArt.rect.size * LivesTimesScale;
            float number = 0f;
            foreach (Sprite n in numbers)
                number = Mathf.Max(number, n.rect.width);
            Vector2 numberBox = new Vector2(number * LivesNumberHeight / numbers[2].rect.height, LivesNumberHeight);
            float width = hero.x + 70f + times.x + 50f + numberBox.x;
            float x = -width * 0.5f;
            Picture(Rect("Hero", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(x + hero.x * 0.5f, 0f), hero, new Vector2(0.5f, 0.5f)), heroArt);
            x += hero.x + 70f;
            Image timesImage = Picture(Rect("Times", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(x + times.x * 0.5f, -20f), times, new Vector2(0.5f, 0.5f)), timesArt);
            timesImage.color = Ink;
            x += times.x + 50f;
            Vector2 numberCentre = new Vector2(x + numberBox.x * 0.5f, 0f);
            Image oldNumber = Picture(Rect("Old Number", panel.transform, new Vector2(0.5f, 0.5f), numberCentre, numberBox, new Vector2(0.5f, 0.5f)), numbers[2]);
            Image newNumber = Picture(Rect("Number", panel.transform, new Vector2(0.5f, 0.5f), numberCentre, numberBox, new Vector2(0.5f, 0.5f)), numbers[2]);
            Vector2 strikeSize = strikeArt.rect.size * (LivesNumberHeight * 1.15f / strikeArt.rect.height);
            Image strike = Picture(Rect("Strike", panel.transform, new Vector2(0.5f, 0.5f), numberCentre, strikeSize, new Vector2(0.5f, 0.5f)), strikeArt);
            // Drawn on from the top, like a pen stroke.
            strike.type = Image.Type.Filled;
            strike.fillMethod = Image.FillMethod.Vertical;
            strike.fillOrigin = (int)Image.OriginVertical.Top;

            var screen = Get<LivesScreen>(hud);
            SetObject(screen, "panel", panel);
            SetObject(screen, "digit", newNumber);
            SetObject(screen, "oldDigit", oldNumber);
            SetObject(screen, "strike", strike);
            var serialized = new SerializedObject(screen);
            SerializedProperty list = serialized.FindProperty("numbers");
            list.arraySize = numbers.Length;
            for (int i = 0; i < numbers.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = numbers[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Transform title = hud.Find("Title Menu");
            panel.transform.SetSiblingIndex(title != null ? title.GetSiblingIndex() : hud.childCount - 1);
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

        /// <summary>
        /// The controls sheet in a small window in the bottom-left corner, shown when play first
        /// starts and faded out by ControlsHint once the keys have been used.
        /// </summary>
        internal static void BuildControlsHint(Transform hud)
        {
            if (hud.Find("Controls Hint") != null)
                return;
            Vector2 size = Window * ControlsHintScale;
            RectTransform card = Rect("Controls Hint", hud, Vector2.zero, new Vector2(30f, 30f) + size * 0.5f, size, new Vector2(0.5f, 0.5f));
            card.gameObject.AddComponent<CanvasGroup>();
            Picture(Rect("Back", card, new Vector2(0.5f, 0.5f), Vector2.zero, size, new Vector2(0.5f, 0.5f)), Art("Window"));
            ControlsRows(card, ControlsHintScale, interactive: false);
            card.gameObject.AddComponent<ControlsHint>();
            // With the HUD, under the menus.
            card.SetSiblingIndex(hud.Find("Hearts").GetSiblingIndex() + 1);
        }

        /// <summary>
        /// Changeable keys: the title's Controls button and Controls screen, and the key boxes in
        /// the pause screen's controls window and the hint card, in place of the old fixed picture
        /// of the controls. Each part is only added if it isn't there yet.
        /// </summary>
        internal static void BuildKeyControls(Transform hud)
        {
            font = FindFont();
            Transform title = hud.Find("Title Menu");
            if (title != null && title.Find("Board/Controls") == null)
                AddTitleControls(hud, title);
            foreach (string path in new[] { "Pause Menu/Controls", "Controls Hint" })
            {
                Transform window = hud.Find(path);
                Transform oldPicture = window != null ? window.Find("Keys") : null;
                if (oldPicture == null || !oldPicture.TryGetComponent(out Image _))
                    continue;
                Object.DestroyImmediate(oldPicture.gameObject);
                bool pause = path.StartsWith("Pause");
                ControlsRows((RectTransform)window, pause ? PauseWindowScale : ControlsHintScale, interactive: pause);
            }
            // The hint card was drawn at a smaller size before the keys became text.
            var hint = (RectTransform)hud.Find("Controls Hint");
            Vector2 hintSize = Window * ControlsHintScale;
            if (hint != null && hint.sizeDelta != hintSize)
            {
                hint.sizeDelta = hintSize;
                hint.anchoredPosition = new Vector2(30f, 30f) + hintSize * 0.5f;
                ((RectTransform)hint.Find("Back")).sizeDelta = hintSize;
            }
        }

        // The title's buttons move to make room for Controls (between Continue and Exit, zig-zagging
        // like the others), and its Controls screen goes over the title.
        static void AddTitleControls(Transform hud, Transform title)
        {
            float k = ReferenceHeight / TitleBoard.y;
            var board = (RectTransform)title.Find("Board");
            MovePiece(board.Find("Start"), new Vector2(120f, 400f), k);
            MovePiece(board.Find("Continue"), new Vector2(680f, 640f), k);
            MovePiece(board.Find("Exit"), new Vector2(700f, 1100f), k);
            Button controls = ButtonPiece("Controls", board, Art("Title/Controls"), new Vector2(150f, 890f), k);
            controls.transform.SetSiblingIndex(board.Find("Exit").GetSiblingIndex());

            GameObject screen = FullScreen("Controls Screen", title, TitleBackground);
            float s = WindowScale;
            RectTransform window = Board("Window", screen.transform, Window * s, new Vector2(0f, 80f));
            Piece("Back", window, Art("Window"), Vector2.zero, s);
            ControlsRows(window, s, interactive: true);
            Sprite returnArt = Art("Controls/Return");
            RectTransform back = Rect("Return", screen.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -448f), returnArt.rect.size * 0.3f, new Vector2(0.5f, 0.5f));
            Button returnButton = MakeButton(Picture(back, returnArt), Navigation.Mode.Automatic);

            var menu = Get<TitleMenu>(hud);
            SetObject(menu, "controlsButton", controls);
            SetObject(menu, "controlsPanel", screen);
            SetObject(menu, "returnButton", returnButton);
        }

        static void MovePiece(Transform piece, Vector2 topLeft, float scale)
        {
            var rect = (RectTransform)piece;
            rect.anchoredPosition = new Vector2(topLeft.x * scale + rect.sizeDelta.x * 0.5f, -(topLeft.y * scale + rect.sizeDelta.y * 0.5f));
        }

        /// <summary>
        /// The rows of the controls window, at scale s of the window drawing: key boxes, arrow and
        /// word. Interactive ones are buttons that wait for a new key, with the "press a key" line
        /// and Reset under them; the hint card's only show the keys, centred in the card.
        /// </summary>
        static RectTransform ControlsRows(RectTransform window, float s, bool interactive)
        {
            RectTransform sheet = Rect("Keys", window, new Vector2(0.5f, 0.5f), Vector2.zero, Window * s, new Vector2(0.5f, 0.5f));
            Sprite keyArt = KeyArt();
            Sprite arrowArt = Art("Controls/Arrow");
            float referencePixelsPerUnit = window.GetComponentInParent<CanvasScaler>().referencePixelsPerUnit;
            // Without Reset and the prompt the rows are centred in the window.
            float top = interactive ? RowTop : (Window.y - RowStep * (ControlRows.Length - 1)) * 0.5f;

            for (int row = 0; row < ControlRows.Length; row++)
            {
                (KeyBindings.Control[] keys, string word) = ControlRows[row];
                float y = top + row * RowStep;
                RectTransform rightBox = null;
                for (int i = keys.Length - 1; i >= 0; i--)
                {
                    RectTransform box = KeyBox(sheet, keys[i], keyArt, new Vector2(KeyRight, y), s, referencePixelsPerUnit, interactive);
                    if (rightBox != null)
                    {
                        var serialized = new SerializedObject(box.GetComponent<KeyButton>());
                        serialized.FindProperty("leftOf").objectReferenceValue = rightBox;
                        serialized.FindProperty("gap").floatValue = KeyGap * s;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    rightBox = box;
                }
                Vector2 arrowSize = arrowArt.rect.size * WordScale;
                AtCentre("Arrow", sheet, arrowArt, new Vector2(ArrowLeft + arrowSize.x * 0.5f, y), WordScale, s);
                Sprite wordArt = Art("Controls/" + word);
                Vector2 wordSize = wordArt.rect.size * WordScale;
                // A little high, as the words' tails (J, P) hang below the line.
                AtCentre(word, sheet, wordArt, new Vector2(ArrowLeft + arrowSize.x + WordGap + wordSize.x * 0.5f, y - wordSize.y * 0.05f), WordScale, s);
            }

            if (!interactive)
            {
                sheet.gameObject.AddComponent<ControlsSheet>();
                return sheet;
            }
            Text prompt = Label("Prompt", sheet, "", Mathf.RoundToInt(46f * s), Ink, TextAnchor.MiddleCenter);
            prompt.fontStyle = FontStyle.Bold;
            Place(prompt.rectTransform, new Vector2(0f, 1f), new Vector2(Window.x * 0.5f * s, -PromptY * s), new Vector2(Window.x * s, 70f * s), new Vector2(0.5f, 0.5f));
            Image reset = AtCentre("Reset", sheet, Art("Controls/Reset"), new Vector2(Window.x * 0.5f, ResetY), SmallButtonScale, s);
            Button resetButton = MakeButton(reset, Navigation.Mode.Automatic);

            var controlsSheet = sheet.gameObject.AddComponent<ControlsSheet>();
            SetObject(controlsSheet, "prompt", prompt);
            SetObject(controlsSheet, "resetButton", resetButton);
            return sheet;
        }

        // A key box: the drawn START frame stretched to fit the key's name, right edge at rightMiddle.
        static RectTransform KeyBox(RectTransform sheet, KeyBindings.Control control, Sprite art, Vector2 rightMiddle, float s, float referencePixelsPerUnit, bool interactive)
        {
            float height = KeyHeight * s;
            RectTransform box = Rect(control.ToString(), sheet, new Vector2(0f, 1f), new Vector2(rightMiddle.x * s, -rightMiddle.y * s), new Vector2(height * 1.25f, height), new Vector2(1f, 0.5f));
            Image image = Picture(box, art);
            image.preserveAspect = false;
            image.type = Image.Type.Sliced;
            // The frame drawn at the box's height, as if the whole drawing were scaled to it.
            image.pixelsPerUnitMultiplier = referencePixelsPerUnit / art.pixelsPerUnit * art.rect.height / height;

            Text label = Label("Key", box, "", Mathf.RoundToInt(KeyHeight * 0.55f * s), Ink, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -height * 0.03f), Vector2.zero, new Vector2(0.5f, 0.5f));
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            // Thickens the letters towards the drawn words' brush weight.
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(1f, -1f) * Mathf.Max(0.8f, 2f * s);

            var key = box.gameObject.AddComponent<KeyButton>();
            var serialized = new SerializedObject(key);
            serialized.FindProperty("control").intValue = (int)control;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("minWidth").floatValue = height * 1.25f;
            serialized.FindProperty("padding").floatValue = height * 0.75f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (interactive)
                MakeButton(image, Navigation.Mode.Automatic);
            return box;
        }

        static Sprite KeyArt()
        {
            Sprite art = Art("Controls/Key");
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtFolder + "Controls/Key.png");
            if (importer.spriteBorder != KeyFrame)
            {
                importer.spriteBorder = KeyFrame;
                importer.SaveAndReimport();
                art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "Controls/Key.png");
            }
            return art;
        }

        // A drawing placed by its centre, in pixels on the window drawing, at artScale of its size.
        static Image AtCentre(string name, RectTransform sheet, Sprite sprite, Vector2 centre, float artScale, float s) =>
            Picture(Rect(name, sheet, new Vector2(0f, 1f), new Vector2(centre.x * s, -centre.y * s), sprite.rect.size * artScale * s, new Vector2(0.5f, 0.5f)), sprite);

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
            ControlsRows(controls, s, interactive: true);

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

        static Button ButtonPiece(string name, RectTransform board, Sprite sprite, Vector2 topLeft, float scale) =>
            MakeButton(Piece(name, board, sprite, topLeft, scale), Navigation.Mode.Vertical);

        // Up / down only on the menus (skipping buttons that can't be used, like Continue with no
        // run); any direction on the controls window, where keys sit side by side.
        static Button MakeButton(Image image, Navigation.Mode navigation)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 0.95f, 0.78f);
            colors.pressedColor = new Color(0.9f, 0.84f, 0.68f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
            button.navigation = new Navigation { mode = navigation };
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
