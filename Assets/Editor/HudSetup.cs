using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Builds Level1's UI into its HUD canvas: hearts (top left), goal text (top centre), the
    /// boss bar moved to the bottom, a speech bubble, and the title, pause and win screens; plus an
    /// EventSystem for the buttons, a goal trigger where the sewers start, and the joke sign's
    /// speech trigger. Only adds what isn't there yet. All text uses the first font found in
    /// Assets/Art/Fonts/ (drop the hand-drawn font there), or Unity's built-in one.
    /// </summary>
    public static class HudSetup
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string UiArtFolder = "Assets/Art/UI/";
        const string FontFolder = "Assets/Art/Fonts";

        const string ControlsText =
            "A / D  or  arrow keys  -  run\n" +
            "W  or  Up  -  jump (hold to jump higher)\n" +
            "Space  -  throw a paper ball\n" +
            "Ctrl  or  C  -  crouch through low gaps\n" +
            "Walk into a crate  -  push it\n" +
            "Esc  -  pause";
        const string JokeLine = "NO RATS ALLOWED.\n(The rats can't read.)";

        static readonly Color Ink = new Color(0.17f, 0.17f, 0.2f);
        static readonly Color Paper = new Color(0.98f, 0.97f, 0.94f);
        static Font font;

        [MenuItem("Tools/NoRegRats/Set Up HUD")]
        public static void Run()
        {
            Sprite heartFull = ConfigureUiSprite("HeartFull");
            Sprite heartEmpty = ConfigureUiSprite("HeartEmpty");
            Sprite bubbleSprite = ConfigureUiSprite("SpeechBubble");
            font = FindFont();

            var scene = EditorSceneManager.OpenScene(ScenePath);
            Transform hud = GameObject.Find("HUD").transform;
            StyleBossBar((RectTransform)hud.Find("Boss Bar"));
            if (hud.Find("Hearts") != null)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("HudSetup: the HUD is already set up.");
                return;
            }
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var playerHealth = player.GetComponent<Health>();

            BuildHearts(hud, playerHealth, heartFull, heartEmpty);
            GoalText goal = BuildGoal(hud);
            SpeechBubble speech = BuildSpeechBubble(hud, bubbleSprite);
            BuildTitle(hud);
            BuildPause(hud);
            BuildWin(hud, playerHealth);
            BuildEventSystem();
            BuildLevelTriggers(goal, speech);

            var arena = Object.FindAnyObjectByType<BossArena>();
            var serialized = new SerializedObject(arena);
            serialized.FindProperty("goalText").objectReferenceValue = goal;
            serialized.FindProperty("speechBubble").objectReferenceValue = speech;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Draw the menus over everything else on the HUD.
            foreach (string menu in new[] { "Win Screen", "Pause Menu", "Title Menu" })
                hud.Find(menu).SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("HudSetup: done.");
        }

        // --- HUD pieces ---

        // The boss bar sits at the bottom (the goal has the top), over the dark floor or the paper,
        // so its label is light with a dark outline and its back gets a light border.
        static void StyleBossBar(RectTransform bossBar)
        {
            bossBar.anchorMin = bossBar.anchorMax = new Vector2(0.5f, 0f);
            bossBar.pivot = new Vector2(0.5f, 0f);
            bossBar.anchoredPosition = new Vector2(0f, 40f);

            Text label = bossBar.Find("Label").GetComponent<Text>();
            label.color = Paper;
            if (!label.TryGetComponent(out Outline labelOutline))
                labelOutline = label.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = Ink;
            labelOutline.effectDistance = new Vector2(3f, -3f);

            GameObject back = bossBar.Find("Back").gameObject;
            if (!back.TryGetComponent(out Outline backOutline))
                backOutline = back.AddComponent<Outline>();
            backOutline.effectColor = Paper;
            backOutline.effectDistance = new Vector2(4f, -4f);
        }

        static void BuildHearts(Transform hud, Health playerHealth, Sprite full, Sprite empty)
        {
            RectTransform hearts = Rect("Hearts", hud, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(600f, 80f), new Vector2(0f, 1f));
            var row = Rect("Row", hearts, new Vector2(0f, 1f), Vector2.zero, new Vector2(400f, 80f), new Vector2(0f, 1f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            RectTransform template = Rect("Heart", row, new Vector2(0f, 1f), Vector2.zero, new Vector2(78f, 70f), new Vector2(0f, 1f));
            var heartImage = template.gameObject.AddComponent<Image>();
            heartImage.sprite = full;
            heartImage.preserveAspect = true;

            Text popup = Label("Bonus Popup", hearts, "+1 heart!", 40, new Color(0.8f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
            Place(popup.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -78f), new Vector2(400f, 50f), new Vector2(0f, 1f));

            var display = hearts.gameObject.AddComponent<HeartsDisplay>();
            var serialized = new SerializedObject(display);
            serialized.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            serialized.FindProperty("heartTemplate").objectReferenceValue = heartImage;
            serialized.FindProperty("fullHeart").objectReferenceValue = full;
            serialized.FindProperty("emptyHeart").objectReferenceValue = empty;
            serialized.FindProperty("bonusPopup").objectReferenceValue = popup.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GoalText BuildGoal(Transform hud)
        {
            Text label = Label("Goal", hud, "Goal: Get to the sewers", 44, Ink, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1100f, 70f), new Vector2(0.5f, 1f));
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Paper;
            outline.effectDistance = new Vector2(2f, -2f);
            var goal = label.gameObject.AddComponent<GoalText>();
            SetObject(goal, "label", label);
            return goal;
        }

        static SpeechBubble BuildSpeechBubble(Transform hud, Sprite bubbleSprite)
        {
            RectTransform holder = Rect("Speech", hud, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            // Pivot at the tail's tip (bottom middle of the image) so the tail points at the speaker.
            RectTransform bubble = Rect("Bubble", holder, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 262f), new Vector2(0.47f, 0.02f));
            bubble.gameObject.AddComponent<Image>().sprite = bubbleSprite;
            Text text = Label("Text", bubble, "", 34, Ink, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = new Vector2(0f, 0.25f);
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(34f, 10f);
            text.rectTransform.offsetMax = new Vector2(-34f, -18f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 34;

            var speech = holder.gameObject.AddComponent<SpeechBubble>();
            SetObject(speech, "bubble", bubble);
            SetObject(speech, "label", text);
            return speech;
        }

        static void BuildTitle(Transform hud)
        {
            RectTransform card = Menu("Title Menu", hud, new Vector2(1000f, 700f), out GameObject panel);
            Column(card,
                Label("Title", card, "NO REGRATS", 110, Ink, TextAnchor.MiddleCenter, 150f, FontStyle.Bold),
                Label("Tagline", card, "Make it through the subway and the sewers,\nbeat the Rat King, take his Heart and get out.", 34, Ink, TextAnchor.MiddleCenter, 110f),
                Label("Controls", card, "A/D run  -  W jump  -  Space shoot  -  Ctrl crouch  -  Esc pause", 28, new Color(Ink.r, Ink.g, Ink.b, 0.75f), TextAnchor.MiddleCenter, 60f));
            Button play = MenuButton("Play", card, new Vector2(0f, -210f));
            Button quit = MenuButton("Quit", card, new Vector2(0f, -300f));
            var menu = hud.gameObject.AddComponent<TitleMenu>();
            SetObject(menu, "panel", panel);
            SetObject(menu, "playButton", play);
            SetObject(menu, "quitButton", quit);
        }

        static void BuildPause(Transform hud)
        {
            RectTransform card = Menu("Pause Menu", hud, new Vector2(1000f, 820f), out GameObject panel);
            Column(card,
                Label("Title", card, "PAUSED", 90, Ink, TextAnchor.MiddleCenter, 120f, FontStyle.Bold),
                Label("Controls", card, ControlsText, 34, Ink, TextAnchor.MiddleCenter, 280f));
            Button resume = MenuButton("Resume", card, new Vector2(0f, -200f));
            Button restart = MenuButton("Restart", card, new Vector2(0f, -285f));
            Button quit = MenuButton("Quit", card, new Vector2(0f, -370f));
            var menu = hud.gameObject.AddComponent<PauseMenu>();
            SetObject(menu, "panel", panel);
            SetObject(menu, "resumeButton", resume);
            SetObject(menu, "restartButton", restart);
            SetObject(menu, "quitButton", quit);
        }

        static void BuildWin(Transform hud, Health playerHealth)
        {
            RectTransform card = Menu("Win Screen", hud, new Vector2(1000f, 680f), out GameObject panel);
            Text stats;
            Column(card,
                Label("Title", card, "YOU ESCAPED!", 100, Ink, TextAnchor.MiddleCenter, 140f, FontStyle.Bold),
                stats = Label("Stats", card, "Time\nDeaths\nHearts left", 44, Ink, TextAnchor.MiddleCenter, 200f));
            Button again = MenuButton("Play Again", card, new Vector2(0f, -200f));
            Button quit = MenuButton("Quit", card, new Vector2(0f, -285f));
            var screen = hud.gameObject.AddComponent<WinScreen>();
            SetObject(screen, "panel", panel);
            SetObject(screen, "stats", stats);
            SetObject(screen, "playAgainButton", again);
            SetObject(screen, "quitButton", quit);
            SetObject(screen, "playerHealth", playerHealth);
        }

        static void BuildEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            InputActionAsset actions = InputSystem.actions;
            module.actionsAsset = actions;
            module.point = InputActionReference.Create(actions.FindAction("UI/Point"));
            module.leftClick = InputActionReference.Create(actions.FindAction("UI/Click"));
            module.scrollWheel = InputActionReference.Create(actions.FindAction("UI/ScrollWheel"));
            module.move = InputActionReference.Create(actions.FindAction("UI/Navigate"));
            module.submit = InputActionReference.Create(actions.FindAction("UI/Submit"));
            module.cancel = InputActionReference.Create(actions.FindAction("UI/Cancel"));
        }

        static void BuildLevelTriggers(GoalText goal, SpeechBubble speech)
        {
            var sewersGoal = new GameObject("Sewers Goal");
            sewersGoal.transform.SetParent(GameObject.Find("Level/Sewers").transform, false);
            sewersGoal.transform.position = new Vector3(61f, -18f, 0f);
            var goalArea = sewersGoal.AddComponent<BoxCollider2D>();
            goalArea.isTrigger = true;
            goalArea.size = new Vector2(12f, 4f);
            var goalTrigger = sewersGoal.AddComponent<GoalTrigger>();
            SetObject(goalTrigger, "goalText", goal);

            Transform subway = GameObject.Find("Level/Subway").transform;
            Transform sign = subway.Find("Sign_Subway");
            var joke = new GameObject("Joke Sign Line");
            joke.transform.SetParent(subway, false);
            joke.transform.position = new Vector3(3f, 1.5f, 0f);
            var jokeArea = joke.AddComponent<BoxCollider2D>();
            jokeArea.isTrigger = true;
            jokeArea.size = new Vector2(6f, 3f);
            var speechTrigger = joke.AddComponent<SpeechTrigger>();
            var serialized = new SerializedObject(speechTrigger);
            serialized.FindProperty("speechBubble").objectReferenceValue = speech;
            serialized.FindProperty("speaker").objectReferenceValue = sign;
            serialized.FindProperty("aboveSpeaker").floatValue = 2.3f;
            serialized.FindProperty("line").stringValue = JokeLine;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- UI helpers ---

        // A full-screen dimmed panel with a paper card in the middle; returns the card.
        static RectTransform Menu(string name, Transform hud, Vector2 cardSize, out GameObject panel)
        {
            RectTransform dim = Rect(name, hud, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            dim.anchorMax = Vector2.one;
            dim.gameObject.AddComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.6f);
            RectTransform card = Rect("Card", dim, new Vector2(0.5f, 0.5f), Vector2.zero, cardSize, new Vector2(0.5f, 0.5f));
            card.gameObject.AddComponent<Image>().color = Paper;
            var border = card.gameObject.AddComponent<Outline>();
            border.effectColor = Ink;
            border.effectDistance = new Vector2(6f, -6f);
            panel = dim.gameObject;
            return card;
        }

        // Stacks labels from the top of the card, each as tall as its own height.
        static void Column(RectTransform card, params Text[] labels)
        {
            float y = -40f;
            foreach (Text label in labels)
            {
                float height = label.rectTransform.sizeDelta.y;
                Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(card.sizeDelta.x - 80f, height), new Vector2(0.5f, 1f));
                y -= height + 10f;
            }
        }

        static Button MenuButton(string text, RectTransform card, Vector2 fromCentre)
        {
            RectTransform rect = Rect(text + " Button", card, new Vector2(0.5f, 0.5f), fromCentre, new Vector2(360f, 70f), new Vector2(0.5f, 0.5f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(4f, -4f);
            var button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.6f);
            colors.selectedColor = new Color(1f, 0.93f, 0.6f);
            colors.pressedColor = new Color(0.95f, 0.8f, 0.4f);
            button.colors = colors;
            Text label = Label("Text", rect, text, 38, Ink, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor alignment, float height = 60f, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, height), new Vector2(0.5f, 0.5f));
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
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

        static Sprite ConfigureUiSprite(string name)
        {
            string path = UiArtFolder + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
