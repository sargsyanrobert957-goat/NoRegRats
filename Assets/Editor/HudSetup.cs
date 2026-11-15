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
    /// boss bar moved to the bottom, a speech bubble, the lives counter, the pause button and the
    /// menus (built by MenuSetup); plus an EventSystem for the buttons, a goal trigger where the sewers start, and
    /// the joke sign's speech trigger. Only adds what isn't there yet. All text uses the first font found in
    /// Assets/Art/Fonts/ (drop the hand-drawn font there), or Unity's built-in one.
    /// </summary>
    public static class HudSetup
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";
        const string UiArtFolder = "Assets/Art/UI/";
        const string FontFolder = "Assets/Art/Fonts";

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
            MenuSetup.Build(hud, playerHealth);
            MenuSetup.BuildLives(hud);
            MenuSetup.BuildPauseButton(hud);
            BuildEventSystem();
            BuildLevelTriggers(goal, speech);

            var arena = Object.FindAnyObjectByType<BossArena>();
            var serialized = new SerializedObject(arena);
            serialized.FindProperty("goalText").objectReferenceValue = goal;
            serialized.FindProperty("speechBubble").objectReferenceValue = speech;
            serialized.ApplyModifiedPropertiesWithoutUndo();

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
