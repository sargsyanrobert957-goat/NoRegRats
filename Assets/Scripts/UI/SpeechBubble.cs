using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// A speech bubble on the HUD that hangs above someone in the world (the Rat King, a sign) for a
    /// few seconds, following them on screen. If the speaker disappears mid-line (the beaten King),
    /// it stays where they last were. One line at a time; a new line replaces the old one.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        [Tooltip("The bubble, anchored at the canvas centre with its pivot at the tail's tip.")]
        [SerializeField] RectTransform bubble;
        [SerializeField] Text label;
        [Tooltip("Keeps the bubble this far inside the screen edges, in canvas units.")]
        [SerializeField] float screenMargin = 20f;

        RectTransform canvasRect;
        Transform speaker;
        Vector3 speakerPosition;
        float height;
        float hideTime;

        void Awake()
        {
            canvasRect = (RectTransform)GetComponentInParent<Canvas>().transform;
            bubble.gameObject.SetActive(false);
        }

        /// <param name="aboveSpeaker">Height above the speaker's position (their feet) for the tail, in world units.</param>
        public void Say(Transform speaker, float aboveSpeaker, string line, float seconds)
        {
            this.speaker = speaker;
            speakerPosition = speaker.position;
            height = aboveSpeaker;
            label.text = line;
            hideTime = Time.unscaledTime + seconds;
            bubble.gameObject.SetActive(true);
            Place();
        }

        void LateUpdate()
        {
            if (!bubble.gameObject.activeSelf)
                return;
            if (Time.unscaledTime >= hideTime)
            {
                bubble.gameObject.SetActive(false);
                return;
            }
            Place();
        }

        void Place()
        {
            if (speaker != null && speaker.gameObject.activeInHierarchy)
                speakerPosition = speaker.position;
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(speakerPosition + Vector3.up * height);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local);

            // Keep the whole bubble on screen.
            Vector2 half = canvasRect.rect.size * 0.5f;
            Vector2 size = bubble.rect.size;
            local.x = Mathf.Clamp(local.x, -half.x + size.x * 0.5f + screenMargin, half.x - size.x * 0.5f - screenMargin);
            local.y = Mathf.Clamp(local.y, -half.y + screenMargin, half.y - size.y - screenMargin);
            bubble.anchoredPosition = local;
        }
    }
}
