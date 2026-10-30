using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoRegRats
{
    /// <summary>
    /// The hero's reaction to their Health: knocked away from whatever hurt them, and on death the
    /// controls stop and the level restarts after a short pause.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement))]
    public class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Velocity the hero is knocked back with: x away from the attacker, y upward.")]
        [SerializeField] Vector2 knockback = new Vector2(7f, 6f);
        [Tooltip("Seconds the controls are taken away during the knockback.")]
        [SerializeField] float knockbackTime = 0.25f;
        [Tooltip("Seconds between dying and the level restarting.")]
        [SerializeField] float restartDelay = 1f;

        Health health;
        PlayerMovement movement;

        void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
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

        void OnDamaged(int amount, Vector2 source)
        {
            float away = Mathf.Sign(transform.position.x - source.x);
            if (Mathf.Approximately(transform.position.x, source.x))
                away = -movement.FacingDirection;
            movement.Knockback(new Vector2(away * knockback.x, knockback.y), knockbackTime);
        }

        void OnDied()
        {
            movement.enabled = false;
            if (TryGetComponent(out PlayerShooter shooter))
                shooter.enabled = false;
            StartCoroutine(RestartLevel());
        }

        // Reloads the scene, which puts every rat and the hero back where they started.
        IEnumerator RestartLevel()
        {
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
