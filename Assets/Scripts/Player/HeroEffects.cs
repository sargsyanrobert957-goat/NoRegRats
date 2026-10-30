using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The feel of the hero's movement: dust and a sound on take-off, a dust burst and sound on
    /// landing (bigger, with a camera shake, after a long fall), a skid puff when turning around on
    /// the ground, and a small puff and footstep every stride while running.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class HeroEffects : MonoBehaviour
    {
        [SerializeField] Color dustColor = new Color(0.42f, 0.42f, 0.47f, 0.9f);
        [Tooltip("Ground distance between footstep puffs and sounds while running.")]
        [SerializeField] float strideLength = 1.5f;
        [Tooltip("Falling at least this fast (units per second) makes a hard landing: more dust and a camera shake.")]
        [SerializeField] float hardLandingSpeed = 15f;
        [SerializeField] float hardLandingShake = 0.18f;

        PlayerMovement movement;
        Collider2D bodyCollider;
        bool wasGrounded = true;
        int lastFacing;
        float fallSpeed;
        float strideProgress;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            bodyCollider = GetComponent<Collider2D>();
        }

        void OnEnable() => movement.Jumped += OnJumped;
        void OnDisable() => movement.Jumped -= OnJumped;
        void Start() => lastFacing = movement.FacingDirection;

        Vector2 Feet => new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y + 0.1f);

        void OnJumped()
        {
            Sfx.Play(Sfx.Sound.Jump);
            PuffSpawner.Spawn(Feet + Vector2.left * 0.2f, 0.9f, new Vector2(-1.2f, 0.2f), dustColor);
            PuffSpawner.Spawn(Feet + Vector2.right * 0.2f, 0.9f, new Vector2(1.2f, 0.2f), dustColor);
        }

        void Update()
        {
            Vector2 velocity = movement.Velocity;
            bool grounded = movement.IsGrounded;

            if (!grounded)
                fallSpeed = Mathf.Max(fallSpeed, -velocity.y);
            else if (!wasGrounded)
                Land();

            if (grounded && Mathf.Abs(velocity.x) > 1f)
            {
                if (movement.FacingDirection != lastFacing)
                    PuffSpawner.Spawn(Feet - Vector2.right * (0.3f * movement.FacingDirection), 1f, new Vector2(-2f * movement.FacingDirection, 0.4f), dustColor);
                strideProgress += Mathf.Abs(velocity.x) * Time.deltaTime;
                if (strideProgress >= strideLength)
                {
                    strideProgress = 0f;
                    Sfx.Play(Sfx.Sound.Step);
                    PuffSpawner.Spawn(Feet - Vector2.right * (0.25f * movement.FacingDirection), 0.65f, new Vector2(-0.8f * movement.FacingDirection, 0.3f), dustColor);
                }
            }

            wasGrounded = grounded;
            lastFacing = movement.FacingDirection;
        }

        void Land()
        {
            bool hard = fallSpeed >= hardLandingSpeed;
            Sfx.Play(Sfx.Sound.Land);
            int puffs = hard ? 4 : 2;
            float speed = hard ? 3f : 1.6f;
            for (int i = 0; i < puffs; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float spread = 0.15f + 0.15f * (i / 2);
                PuffSpawner.Spawn(Feet + Vector2.right * (side * spread), hard ? 1.4f : 0.9f, new Vector2(side * speed, 0.3f), dustColor);
            }
            if (hard)
                CameraFollow2D.ShakeActive(hardLandingShake, 0.2f);
            fallSpeed = 0f;
        }
    }
}
