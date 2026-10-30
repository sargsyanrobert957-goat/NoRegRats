using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Runs the boss fight. Its trigger covers the arena floor past the entrance; when the hero walks
    /// in, the gate drops behind them, the camera locks onto the arena, the Rat King starts and his
    /// health bar shows. When the hero takes the Heart he drops, the exit grate opens and the camera
    /// is freed. If the Heart was already taken earlier in the run, the exit is simply open.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArena : MonoBehaviour
    {
        [SerializeField] RatKing king;
        [SerializeField] SlidingGate entranceGate;
        [SerializeField] SlidingGate exitGrate;
        [SerializeField] CameraFollow2D cameraFollow;
        [Tooltip("Where the camera is held during the fight.")]
        [SerializeField] Transform cameraSpot;
        [SerializeField] BossHealthBar healthBar;

        bool started;

        void Start()
        {
            if (GameSession.HasHeart)
            {
                started = true;
                exitGrate.Open();
            }
        }

        void OnEnable()
        {
            king.Defeated += OnKingDefeated;
            HeartPickup.Collected += OnHeartCollected;
        }

        void OnDisable()
        {
            king.Defeated -= OnKingDefeated;
            HeartPickup.Collected -= OnHeartCollected;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (started || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerMovement _))
                return;
            started = true;
            entranceGate.Close();
            cameraFollow.LockTo(cameraSpot.position);
            king.StartFight();
            healthBar.Show(king.Health);
        }

        void OnKingDefeated() => healthBar.Hide();

        void OnHeartCollected()
        {
            exitGrate.Open();
            cameraFollow.Unlock();
        }
    }
}
