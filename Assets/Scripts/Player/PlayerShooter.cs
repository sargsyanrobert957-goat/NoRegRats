using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace NoRegRats
{
    /// <summary>
    /// Fires the hero's shot (a paper ball) straight ahead when Attack is pressed (Space), with a
    /// cooldown. Shots come from a pool so firing never creates garbage.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] Projectile projectilePrefab;
        [Tooltip("Seconds between shots.")]
        [SerializeField] float cooldown = 0.35f;
        [Tooltip("Where shots appear, from the middle of the collider. x points the way the hero faces. Keep it low enough to hit rats (about 1 unit tall).")]
        [SerializeField] Vector2 muzzleOffset = new Vector2(0.45f, -0.15f);
        [Tooltip("Gets a small recoil nudge on every shot.")]
        [SerializeField] HeroAnimator heroAnimator;

        PlayerMovement movement;
        Collider2D bodyCollider;
        InputAction attackAction;
        ObjectPool<Projectile> pool;
        float nextShotTime;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            bodyCollider = GetComponent<Collider2D>();
            attackAction = InputSystem.actions.FindAction("Player/Attack", throwIfNotFound: true);
            pool = new ObjectPool<Projectile>(
                () => Instantiate(projectilePrefab),
                shot => shot.gameObject.SetActive(true),
                shot => shot.gameObject.SetActive(false),
                shot => Destroy(shot.gameObject),
                collectionCheck: false, defaultCapacity: 8, maxSize: 16);
        }

        void Update()
        {
            if (attackAction.WasPressedThisFrame() && Time.time >= nextShotTime)
                Shoot();
        }

        void Shoot()
        {
            nextShotTime = Time.time + cooldown;
            int direction = movement.FacingDirection;
            Vector2 origin = (Vector2)bodyCollider.bounds.center + new Vector2(muzzleOffset.x * direction, muzzleOffset.y);
            pool.Get().Launch(origin, direction, gameObject, pool.Release);
            if (heroAnimator != null)
                heroAnimator.Recoil();
        }
    }
}
