using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A bullet from the shooting rat: flies straight at a fixed speed, hurts the hero if it touches
    /// them, passes through other rats and the Rat King, and disappears on any wall or platform or
    /// after its range. Needs a trigger collider and a Dynamic Rigidbody2D with gravity 0 (Dynamic so
    /// it also notices walls and platforms).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBullet : MonoBehaviour
    {
        [SerializeField] float speed = 9f;
        [Tooltip("Distance travelled before the bullet disappears, in world units.")]
        [SerializeField] float range = 16f;
        [Tooltip("Hearts lost when it hits the hero.")]
        [SerializeField] int damage = 1;
        [Tooltip("Spin, in degrees per second (it is a scribble).")]
        [SerializeField] float spin = 720f;

        Rigidbody2D body;
        float despawnTime;

        void Awake() => body = GetComponent<Rigidbody2D>();

        /// <param name="direction">1 = right, -1 = left.</param>
        public void Launch(Vector2 from, int direction)
        {
            transform.position = from;
            body.position = from;
            body.linearVelocity = new Vector2(direction * speed, 0f);
            body.angularVelocity = -spin * direction;
            despawnTime = Time.time + range / speed;
        }

        void Update()
        {
            if (Time.time >= despawnTime)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger)
                return;

            Rigidbody2D hit = other.attachedRigidbody;
            if (hit != null && hit.TryGetComponent(out PlayerMovement _))
            {
                if (hit.TryGetComponent(out Health health))
                    health.TakeDamage(damage, body.position);
            }
            else if (hit != null && hit.TryGetComponent(out Health _))
            {
                // Another rat (or the King) in the way: the bullet goes past.
                return;
            }

            PuffSpawner.Burst(body.position, 4, 0.5f, 2.5f, new Color(0.3f, 0.3f, 0.32f, 0.9f));
            Destroy(gameObject);
        }
    }
}
