using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The way out past the arena's exit grate. Walking into its trigger wins the game: the run is
    /// marked won (the win screen reacts to that) and the hero's controls stop.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (GameSession.HasWon || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerMovement movement))
                return;
            movement.enabled = false;
            if (movement.TryGetComponent(out PlayerShooter shooter))
                shooter.enabled = false;
            other.attachedRigidbody.linearVelocity = Vector2.zero;
            GameSession.Win();
        }
    }
}
