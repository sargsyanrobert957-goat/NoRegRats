using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Hurts the hero while this object's collider is within reach of theirs (a rat's bite). Uses a
    /// distance check so it works with pass-through colliders; the hero's invulnerability limits it to
    /// one bite at a time.
    /// </summary>
    public class ContactDamage : MonoBehaviour
    {
        [Tooltip("Hearts lost per bite.")]
        [SerializeField] int damage = 1;
        [Tooltip("Colliders closer than this count as touching, in world units.")]
        [SerializeField] float reach = 0.02f;
        [Tooltip("Collider that bites. Defaults to the one on this object.")]
        [SerializeField] Collider2D biteCollider;

        Health ownHealth;
        Health playerHealth;
        Collider2D playerCollider;

        void Awake()
        {
            if (biteCollider == null)
                biteCollider = GetComponent<Collider2D>();
            ownHealth = GetComponent<Health>();
        }

        void Start()
        {
            var player = FindAnyObjectByType<PlayerMovement>();
            if (player == null)
                return;
            playerHealth = player.GetComponent<Health>();
            playerCollider = player.GetComponent<Collider2D>();
        }

        void FixedUpdate()
        {
            if (playerHealth == null || !biteCollider.enabled || (ownHealth != null && ownHealth.IsDead))
                return;
            ColliderDistance2D distance = biteCollider.Distance(playerCollider);
            if (distance.isValid && distance.distance <= reach)
                playerHealth.TakeDamage(damage, biteCollider.bounds.center);
        }
    }
}
