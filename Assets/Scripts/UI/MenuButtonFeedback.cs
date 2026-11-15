using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Makes a drawn menu button react: it grows a little and tilts while it's the one picked (by the
    /// mouse or the keyboard / gamepad), squashes while pressed, and plays the Click sound. Hovering
    /// with the mouse also selects it, so mouse and keyboard always agree on which button is lit.
    /// Uses unscaled time, since menus are open while the game is frozen.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Tooltip("Scale while selected.")]
        [SerializeField] float selectedScale = 1.08f;
        [Tooltip("Scale while held down.")]
        [SerializeField] float pressedScale = 0.94f;
        [Tooltip("Tilt while selected, in degrees.")]
        [SerializeField] float selectedTilt = -2f;
        [Tooltip("How quickly it eases to its target. Higher is snappier.")]
        [SerializeField] float spring = 18f;

        Button button;
        Vector3 baseScale;
        Quaternion baseRotation;
        bool selected;
        bool pressed;

        void Awake()
        {
            button = GetComponent<Button>();
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            button.onClick.AddListener(() => Sfx.Play(Sfx.Sound.Click));
        }

        void OnDisable()
        {
            selected = pressed = false;
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnPointerDown(PointerEventData eventData) => pressed = button.interactable;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = pressed = false;

        void Update()
        {
            bool lit = selected && button.interactable;
            float scale = pressed ? pressedScale : lit ? selectedScale : 1f;
            float k = 1f - Mathf.Exp(-spring * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * scale, k);
            Quaternion target = baseRotation * Quaternion.Euler(0f, 0f, lit ? selectedTilt : 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, k);
        }
    }
}
