using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A box the player can push by walking into it on the ground. PlayerMovement detects it and
    /// moves it along at <see cref="PushSpeed"/>; once the player lets go, ground friction stops it,
    /// and pushed off a ledge it simply falls. Boxes stacked on top ride along on friction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PushableBox : MonoBehaviour
    {
        [Tooltip("How fast the player moves while pushing this box. Use lower values for bigger boxes.")]
        [SerializeField] float pushSpeed = 2.5f;

        public float PushSpeed => pushSpeed;

        Rigidbody2D body;

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
        }
    }
}
