using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The exit past the arena grate. Entering its trigger wins the game and stops the hero's controls.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (GameSession.HasWon || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerMovement movement))
                return;
            movement.enabled = false;
            if (movement.TryGetComponent(out PlayerPunch punch))
                punch.enabled = false;
            other.attachedRigidbody.linearVelocity = Vector2.zero;
            GameSession.Win();
        }
    }
}
