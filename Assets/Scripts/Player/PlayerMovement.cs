using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRegRats
{
    /// <summary>
    /// Side-scrolling player movement: walking, running while Sprint is held, and jumping with
    /// coyote time, jump buffering and variable jump height (let go of Jump early for a short hop).
    /// Reads the "Player/Move", "Player/Sprint" and "Player/Jump" actions from the project-wide
    /// input actions (Assets/Settings/InputSystem_Actions.inputactions).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Walking and Running")]
        [SerializeField] float walkSpeed = 4f;
        [Tooltip("Top speed while Sprint is held (Left Shift / left stick press).")]
        [SerializeField] float runSpeed = 8f;
        [Tooltip("Speed gained per second while moving on the ground.")]
        [SerializeField] float groundAcceleration = 90f;
        [Tooltip("Speed lost per second on the ground when there is no input.")]
        [SerializeField] float groundDeceleration = 110f;
        [SerializeField] float airAcceleration = 60f;
        [SerializeField] float airDeceleration = 40f;

        [Header("Jumping")]
        [Tooltip("Height of a full jump (Jump held), in world units.")]
        [SerializeField] float jumpHeight = 3.5f;
        [Tooltip("Rigidbody gravity scale while rising.")]
        [SerializeField] float gravityScale = 4f;
        [Tooltip("Gravity is multiplied by this while falling, so jumps feel snappy instead of floaty.")]
        [SerializeField] float fallGravityMultiplier = 1.6f;
        [Tooltip("Upward speed is multiplied by this when Jump is released early.")]
        [SerializeField, Range(0f, 1f)] float jumpCutMultiplier = 0.4f;
        [SerializeField] float maxFallSpeed = 20f;
        [Tooltip("Seconds after running off a ledge during which a jump is still allowed.")]
        [SerializeField] float coyoteTime = 0.1f;
        [Tooltip("Seconds a Jump press is remembered before landing.")]
        [SerializeField] float jumpBufferTime = 0.12f;

        [Header("Ground Check")]
        [Tooltip("Surfaces sloped up to this many degrees count as ground.")]
        [SerializeField, Range(0f, 89f)] float maxGroundAngle = 50f;

        [Header("Visuals")]
        [Tooltip("Child holding the stick figure. It is mirrored to face the movement direction.")]
        [SerializeField] Transform visual;

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;

        Rigidbody2D body;
        InputAction moveAction;
        InputAction sprintAction;
        InputAction jumpAction;
        ContactFilter2D groundFilter;

        float moveInput;
        bool sprintHeld;
        bool runPace;
        bool jumpHeld;
        float jumpBufferTimer;
        float coyoteTimer;
        bool isRising;
        bool canCutJump;

        float JumpSpeed => Mathf.Sqrt(2f * jumpHeight * gravityScale * -Physics2D.gravity.y);

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError("PlayerMovement: no project-wide input actions are set (Project Settings > Input System Package).", this);
                enabled = false;
                return;
            }
            moveAction = actions.FindAction("Player/Move", throwIfNotFound: true);
            sprintAction = actions.FindAction("Player/Sprint", throwIfNotFound: true);
            jumpAction = actions.FindAction("Player/Jump", throwIfNotFound: true);

            // Ground = any contact whose normal points within maxGroundAngle of straight up.
            groundFilter.SetNormalAngle(90f - maxGroundAngle, 90f + maxGroundAngle);
        }

        // Input is read every frame so quick taps are never missed; physics happens in FixedUpdate.
        void Update()
        {
            moveInput = moveAction.ReadValue<Vector2>().x;
            sprintHeld = sprintAction.IsPressed();
            jumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame())
                jumpBufferTimer = jumpBufferTime;

            if (moveInput > 0.01f)
                FacingDirection = 1;
            else if (moveInput < -0.01f)
                FacingDirection = -1;

            if (visual != null)
            {
                Vector3 scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * FacingDirection;
                visual.localScale = scale;
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 velocity = body.linearVelocity;

            // Right after a jump the feet can still report ground contact for a step, so ignore it while rising.
            IsGrounded = !isRising && body.IsTouching(groundFilter);
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - dt;
            jumpBufferTimer -= dt;

            // Walk or run is chosen on the ground; in the air the player keeps the pace they jumped with.
            if (IsGrounded)
                runPace = sprintHeld;
            float targetSpeed = moveInput * (runPace ? runSpeed : walkSpeed);
            bool hasInput = Mathf.Abs(moveInput) > 0.01f;
            float rate = IsGrounded
                ? (hasInput ? groundAcceleration : groundDeceleration)
                : (hasInput ? airAcceleration : airDeceleration);
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * dt);

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                velocity.y = JumpSpeed;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                isRising = true;
                canCutJump = true;
                IsGrounded = false;
            }

            if (isRising)
            {
                if (velocity.y <= 0f)
                {
                    isRising = false;
                }
                else if (canCutJump && !jumpHeld)
                {
                    velocity.y *= jumpCutMultiplier;
                    canCutJump = false;
                }
            }

            body.gravityScale = velocity.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            body.linearVelocity = velocity;
        }
    }
}
