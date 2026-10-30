using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The hero's reaction to their Health: knocked away from whatever hurt them, and on death the
    /// controls stop, the death is counted and the level fades out and restarts (every rat and the
    /// Rat King come back). If the Heart was already taken, the hero restarts with the extra heart.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement))]
    public class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Velocity the hero is knocked back with: x away from the attacker, y upward.")]
        [SerializeField] Vector2 knockback = new Vector2(7f, 6f);
        [Tooltip("Seconds the controls are taken away during the knockback.")]
        [SerializeField] float knockbackTime = 0.25f;
        [Tooltip("Seconds between dying and the screen starting to fade out.")]
        [SerializeField] float restartDelay = 0.8f;
        [Tooltip("Extra hearts the Heart item gives, kept after dying.")]
        [SerializeField] int heartBonus = 1;
        [Tooltip("Camera shake when hurt, in world units.")]
        [SerializeField] float hurtShake = 0.15f;

        Health health;
        PlayerMovement movement;

        void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
        }

        void Start()
        {
            if (GameSession.HasHeart)
                health.SetMax(health.Max + heartBonus, refill: true);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        /// <summary>The Heart item: one more max heart, refilled.</summary>
        public void GainHeart() => health.SetMax(health.Max + heartBonus, refill: true);

        void OnDamaged(int amount, Vector2 source)
        {
            float away = Mathf.Sign(transform.position.x - source.x);
            if (Mathf.Approximately(transform.position.x, source.x))
                away = -movement.FacingDirection;
            movement.Knockback(new Vector2(away * knockback.x, knockback.y), knockbackTime);
            Sfx.Play(Sfx.Sound.Hurt);
            CameraFollow2D.ShakeActive(hurtShake, 0.2f);
        }

        void OnDied()
        {
            movement.enabled = false;
            if (TryGetComponent(out PlayerShooter shooter))
                shooter.enabled = false;
            GameSession.RecordDeath();
            StartCoroutine(RestartLevel());
        }

        IEnumerator RestartLevel()
        {
            yield return new WaitForSeconds(restartDelay);
            ScreenFader.ReloadScene();
        }
    }
}
