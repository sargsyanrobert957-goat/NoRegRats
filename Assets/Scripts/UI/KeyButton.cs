using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// One drawn key box on the controls sheet, showing a control's current key. With a Button on it,
    /// clicking it (or Enter / Space while it's selected) waits for a new key. The box widens for long
    /// key names, keeping its right edge where it is.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class KeyButton : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] KeyBindings.Control control;
        [Tooltip("Plain-font key name: used if there is no drawn label, and by it for what it can't draw.")]
        [SerializeField] Text label;
        [Tooltip("The key name in the hand-drawn capitals.")]
        [SerializeField] DrawnText drawnLabel;
        [Tooltip("Narrowest the box gets, in canvas units.")]
        [SerializeField] float minWidth = 100f;
        [Tooltip("Room left and right of the key's name, in canvas units, both sides together.")]
        [SerializeField] float padding = 60f;
        [Tooltip("Another key box this one sits to the left of (A next to D). Empty: it stays where it was placed.")]
        [SerializeField] RectTransform leftOf;
        [Tooltip("Space between this box and the one it sits next to.")]
        [SerializeField] float gap = 10f;
        [Tooltip("Shown on the box while waiting for a key.")]
        [SerializeField] string waitingText = "?";

        RectTransform rect;
        Button button;

        public KeyBindings.Control Control => control;

        void Awake()
        {
            rect = (RectTransform)transform;
            button = GetComponent<Button>();
        }

        void OnEnable()
        {
            KeyBindings.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => KeyBindings.Changed -= Refresh;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Choose();
        }

        // Enter or Space on the selected box. Ignored straight after a key was chosen with that press.
        public void OnSubmit(BaseEventData eventData)
        {
            if (!KeyBindings.BlocksMenuKeys)
                Choose();
        }

        void Choose()
        {
            if (button == null || !button.interactable)
                return;
            KeyBindings.Rebind(control);
        }

        void Refresh()
        {
            string name = KeyBindings.Rebinding == control ? waitingText : KeyBindings.KeyName(control);
            float width;
            if (drawnLabel != null)
            {
                drawnLabel.SetText(name);
                width = drawnLabel.Width;
            }
            else
            {
                label.text = name;
                width = label.preferredWidth;
            }
            rect.sizeDelta = new Vector2(Mathf.Max(minWidth, width + padding), rect.sizeDelta.y);
        }

        void LateUpdate()
        {
            if (leftOf == null)
                return;
            Vector2 position = rect.anchoredPosition;
            position.x = leftOf.anchoredPosition.x - leftOf.rect.width * leftOf.pivot.x - gap - rect.rect.width * (1f - rect.pivot.x);
            rect.anchoredPosition = position;
        }
    }
}
