using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The lives left, under the hearts. After a death (the level has just reloaded with a life
    /// fewer) it pops and flashes for a moment so the loss is noticed.
    /// </summary>
    public class LivesDisplay : MonoBehaviour
    {
        [SerializeField] Text label;
        [Tooltip("Colour of the flash after losing a life.")]
        [SerializeField] Color lostColor = new Color(0.8f, 0.2f, 0.2f);
        [Tooltip("Seconds the pop and flash last.")]
        [SerializeField] float popTime = 1.5f;
        [Tooltip("How much bigger the label gets at the start of the pop.")]
        [SerializeField] float popScale = 1.4f;

        Color normalColor;
        float popUntil;

        void Awake() => normalColor = label.color;

        void Start()
        {
            label.text = "Lives: " + GameSession.Lives;
            if (GameSession.Lives < GameSession.StartingLives)
                popUntil = Time.unscaledTime + popTime;
        }

        void Update()
        {
            float left = Mathf.Clamp01((popUntil - Time.unscaledTime) / popTime);
            label.transform.localScale = Vector3.one * Mathf.Lerp(1f, popScale, left * left);
            bool blink = left > 0f && Mathf.Repeat(Time.unscaledTime * 4f, 1f) < 0.5f;
            label.color = blink ? lostColor : normalColor;
        }
    }
}
