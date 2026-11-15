using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A box the hero pushes by walking into it. PlayerMovement moves it at a fixed speed while pushed;
    /// otherwise friction stops it, and it falls off ledges.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PushableBox : MonoBehaviour
    {
        [Tooltip("How fast the player moves while pushing this box. Use lower values for bigger boxes.")]
        [SerializeField] float pushSpeed = 2.5f;

        [Tooltip("Seconds between repeats of the scrape sound while the box is being pushed.")]
        [SerializeField] float scrapeInterval = 0.45f;

        public float PushSpeed => pushSpeed;

        Rigidbody2D body;
        float nextScrape;

        void Reset() => ApplyDefaultBodySettings();

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
        }

        /// <summary>Sets the Rigidbody2D up as a heavy, non-rotating box. Called when the component is added.</summary>
        public void ApplyDefaultBodySettings()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 5f;
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        /// <summary>Moves the box horizontally at the pusher's speed for this physics step.</summary>
        public void Push(float velocityX)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = velocityX;
            body.linearVelocity = velocity;

            // The scrape repeats while the box is being pushed along.
            if (Mathf.Abs(velocityX) > 0.1f && Time.time >= nextScrape)
            {
                nextScrape = Time.time + scrapeInterval;
                Sfx.Play(Sfx.Sound.Push);
            }
        }
    }
}
