using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Makes something in the world say a line (in the SpeechBubble) the first time the hero walks
    /// into its trigger, e.g. the joke sign at the start.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SpeechTrigger : MonoBehaviour
    {
        [SerializeField] SpeechBubble speechBubble;
        [Tooltip("Who or what is talking; the bubble hangs above it.")]
        [SerializeField] Transform speaker;
        [Tooltip("Height of the bubble's tail above the speaker's position, in world units.")]
        [SerializeField] float aboveSpeaker = 2f;
        [SerializeField, TextArea] string line;
        [SerializeField] float seconds = 4f;

        bool said;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (said || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerMovement _))
                return;
            said = true;
            speechBubble.Say(speaker, aboveSpeaker, line, seconds);
        }
    }
}
