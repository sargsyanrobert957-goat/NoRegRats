using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The controls window (title's Controls screen and the pause screen): the key boxes
    /// (<see cref="KeyButton"/>), a line saying how to pick a key while one is being changed, and
    /// Reset. A click anywhere else stops waiting for a key, and so does closing the window.
    /// </summary>
    public class ControlsSheet : MonoBehaviour
    {
        [Tooltip("Shown while waiting for a key. Can be empty.")]
        [SerializeField] Text prompt;
        [Tooltip("Puts every key back to its default. Can be empty.")]
        [SerializeField] Button resetButton;
        [SerializeField] string pressKeyText = "Press a key  (Esc to cancel)";
        [SerializeField] string pressPauseKeyText = "Press a key  (click to cancel)";

        void Awake()
        {
            if (resetButton != null)
                resetButton.onClick.AddListener(KeyBindings.ResetToDefaults);
        }

        void OnEnable()
        {
            KeyBindings.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            KeyBindings.Changed -= Refresh;
            KeyBindings.CancelRebind();
        }

        void Update()
        {
            // The press cancels; if it lands on a key box, that box starts waiting on release.
            if (KeyBindings.IsRebinding && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                KeyBindings.CancelRebind();
        }

        void Refresh()
        {
            if (prompt == null)
                return;
            KeyBindings.Control? waiting = KeyBindings.Rebinding;
            prompt.text = waiting == null ? "" : waiting == KeyBindings.Control.Pause ? pressPauseKeyText : pressKeyText;
        }
    }
}
