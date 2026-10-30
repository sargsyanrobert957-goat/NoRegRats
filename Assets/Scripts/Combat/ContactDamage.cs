using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Hurts the hero whenever this object's collider touches theirs (a rat's bite). Uses a distance
    /// check rather than collision events, so it still works when the two colliders are set to pass
    /// through each other. The hero's invulnerability after a hit stops it from biting every frame.
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
