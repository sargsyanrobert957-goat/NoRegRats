using System;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A shot that flies straight at a fixed speed, damages the first Health it touches and
    /// disappears on any solid hit or after travelling its range. Needs a trigger collider and a
    /// Dynamic Rigidbody2D with gravity 0 (Dynamic so it also notices walls and platforms).
    /// Spawned and recycled by a pool, e.g. PlayerShooter's.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] float speed = 14f;
        [Tooltip("Distance travelled before the shot disappears, in world units.")]
        [SerializeField] float range = 6f;
        [SerializeField] int damage = 1;

        Rigidbody2D body;
        GameObject owner;
        Action<Projectile> release;
        float despawnTime;
        bool released;

        void Awake() => body = GetComponent<Rigidbody2D>();

        /// <param name="owner">Never hit by this shot (the shooter).</param>
        /// <param name="release">Called when the shot is done, to return it to its pool.</param>
        public void Launch(Vector2 position, int direction, GameObject owner, Action<Projectile> release)
        {
            this.owner = owner;
            this.release = release;
            released = false;
            transform.position = position;
            body.position = position;
            body.linearVelocity = new Vector2(direction * speed, 0f);
            despawnTime = Time.time + range / speed;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        void Update()
        {
            if (Time.time >= despawnTime)
                Despawn();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (released || other.isTrigger)
                return;
            GameObject hit = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (hit == owner)
                return;

            if (hit.TryGetComponent(out Health health))
                health.TakeDamage(damage, body.position);
            Despawn();
        }

        void Despawn()
        {
            if (released)
                return;
            released = true;
            body.linearVelocity = Vector2.zero;
            if (release != null)
                release(this);
            else
                Destroy(gameObject);
        }
    }
}
