using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The Rat King's thrown trash: launched on an arc that lands on its target, spinning. It flies
    /// through platforms (so the arena's bars aren't a permanent shield) and only hits the hero; if
    /// it misses, it disappears once it falls just below where the hero stood, about floor level.
    /// Needs a trigger collider and a Dynamic Rigidbody2D (its gravity scale shapes the arc).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class TrashProjectile : MonoBehaviour
    {
        [Tooltip("Hearts lost when it hits the hero.")]
        [SerializeField] int damage = 1;
        [Tooltip("How far below the target point it falls before disappearing, in world units.")]
        [SerializeField] float missDepth = 0.9f;
        [Tooltip("Seconds before it disappears whatever happens.")]
        [SerializeField] float lifetime = 4f;
        [Tooltip("Spin, in degrees per second.")]
        [SerializeField] float spin = 360f;

        Rigidbody2D body;
        float despawnTime;
        float missY;

        void Awake() => body = GetComponent<Rigidbody2D>();

        /// <summary>Throws from <paramref name="from"/> so it lands on <paramref name="target"/> after <paramref name="flightTime"/> seconds.</summary>
        public void Launch(Vector2 from, Vector2 target, float flightTime)
        {
            missY = target.y - missDepth;
            float gravity = -Physics2D.gravity.y * body.gravityScale;
            var velocity = new Vector2(
                (target.x - from.x) / flightTime,
                (target.y - from.y + 0.5f * gravity * flightTime * flightTime) / flightTime);
            transform.position = from;
            body.position = from;
            body.linearVelocity = velocity;
            body.angularVelocity = -spin * Mathf.Sign(velocity.x);
            despawnTime = Time.time + lifetime;
        }

        void Update()
        {
            bool missed = body.linearVelocity.y < 0f && body.position.y < missY;
            if (missed || Time.time >= despawnTime)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D hit = other.attachedRigidbody;
            if (other.isTrigger || hit == null || !hit.TryGetComponent(out PlayerMovement _) || !hit.TryGetComponent(out Health health))
                return;
            health.TakeDamage(damage, body.position);
            PuffSpawner.Burst(body.position, 5, 0.7f, 2.5f, new Color(0.55f, 0.57f, 0.6f, 0.9f));
            Destroy(gameObject);
        }
    }
}
