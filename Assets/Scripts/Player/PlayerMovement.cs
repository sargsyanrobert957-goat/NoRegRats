using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRegRats
{
    /// <summary>
    /// Side-scrolling player movement: running at one speed, crouching while Crouch is held, pushing
    /// PushableBoxes by walking into them, and jumping with coyote time, jump buffering and variable
    /// jump height (let go of Jump early for a short hop).
    /// Reads the "Player/Move", "Player/Jump" and "Player/Crouch" actions from the project-wide
    /// input actions (Assets/Settings/InputSystem_Actions.inputactions). Facing and visuals are
    /// handled by HeroAnimator.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Running")]
        [Tooltip("Top speed on the ground and in the air.")]
        [SerializeField] float moveSpeed = 6f;
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

        [Header("Crouching")]
        [Tooltip("Top speed while Crouch is held (Left Ctrl / C).")]
        [SerializeField] float crouchSpeed = 2f;
        [Tooltip("Collider height while crouching. The bottom of the collider stays where it is.")]
        [SerializeField] float crouchHeight = 1.2f;

        [Header("Pushing")]
        [Tooltip("How close a PushableBox must be in front of the player to be pushed, in world units.")]
        [SerializeField] float pushReach = 0.05f;

        [Header("Ground Check")]
        [Tooltip("Surfaces sloped up to this many degrees count as ground.")]
        [SerializeField, Range(0f, 89f)] float maxGroundAngle = 50f;

        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsPushing { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;
        public float MoveSpeed => moveSpeed;
        public float CrouchSpeed => crouchSpeed;

        Rigidbody2D body;
        CapsuleCollider2D capsule;
        Vector2 standingSize;
        Vector2 standingOffset;
        InputAction moveAction;
        InputAction jumpAction;
        InputAction crouchAction;
        ContactFilter2D groundFilter;
        ContactFilter2D ceilingFilter;
        ContactFilter2D solidFilter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];

        float moveInput;
        bool crouchHeld;
        bool jumpHeld;
        float jumpBufferTimer;
        float coyoteTimer;
        bool isRising;
        bool canCutJump;

        float JumpSpeed => Mathf.Sqrt(2f * jumpHeight * gravityScale * -Physics2D.gravity.y);

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            standingSize = capsule.size;
            standingOffset = capsule.offset;

            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError("PlayerMovement: no project-wide input actions are set (Project Settings > Input System Package).", this);
                enabled = false;
                return;
            }
            moveAction = actions.FindAction("Player/Move", throwIfNotFound: true);
            jumpAction = actions.FindAction("Player/Jump", throwIfNotFound: true);
            crouchAction = actions.FindAction("Player/Crouch", throwIfNotFound: true);

            // Ground = any contact whose normal points within maxGroundAngle of straight up.
            groundFilter.SetNormalAngle(90f - maxGroundAngle, 90f + maxGroundAngle);
            // Ceiling = any surface facing down, mirrored from the ground check.
            ceilingFilter.SetNormalAngle(270f - maxGroundAngle, 270f + maxGroundAngle);
        }

        // Input is read every frame so quick taps are never missed; physics happens in FixedUpdate.
        void Update()
        {
            moveInput = moveAction.ReadValue<Vector2>().x;
            crouchHeld = crouchAction.IsPressed();
            jumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame())
                jumpBufferTimer = jumpBufferTime;

            if (moveInput > 0.01f)
                FacingDirection = 1;
            else if (moveInput < -0.01f)
                FacingDirection = -1;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 velocity = body.linearVelocity;

            // Right after a jump the feet can still report ground contact for a step, so ignore it while rising.
            IsGrounded = !isRising && body.IsTouching(groundFilter);
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - dt;
            jumpBufferTimer -= dt;

            // Crouching starts only on the ground. Standing back up waits until there is room overhead,
            // so letting go of Crouch in a low tunnel keeps the player crouched until they are out.
            if (crouchHeld && IsGrounded)
                SetCrouching(true);
            else if (IsCrouching && HasHeadroom())
                SetCrouching(false);

            float maxSpeed = IsCrouching ? crouchSpeed : moveSpeed;
            bool hasInput = Mathf.Abs(moveInput) > 0.01f;

            PushableBox box = IsGrounded && hasInput ? FindPushableBox() : null;
            IsPushing = box != null;
            if (IsPushing)
                maxSpeed = Mathf.Min(maxSpeed, box.PushSpeed);

            float targetSpeed = moveInput * maxSpeed;
            float rate = IsGrounded
                ? (hasInput ? groundAcceleration : groundDeceleration)
                : (hasInput ? airAcceleration : airDeceleration);
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * dt);
            if (IsPushing)
                box.Push(velocity.x);

            if (jumpBufferTimer > 0f && coyoteTimer > 0f && (!IsCrouching || HasHeadroom()))
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

        // Shrinks the collider from the top, keeping its bottom on the ground.
        void SetCrouching(bool crouch)
        {
            if (IsCrouching == crouch)
                return;
            IsCrouching = crouch;
            float height = crouch ? crouchHeight : standingSize.y;
            capsule.size = new Vector2(standingSize.x, height);
            capsule.offset = standingOffset + Vector2.down * ((standingSize.y - height) * 0.5f);
        }

        // Sweeps the crouched collider up by the height it would grow when standing.
        bool HasHeadroom() => capsule.Cast(Vector2.up, ceilingFilter, hits, standingSize.y - crouchHeight) == 0;

        // A PushableBox directly in front of the player, touching or almost touching their side.
        PushableBox FindPushableBox()
        {
            int count = body.Cast(new Vector2(FacingDirection, 0f), solidFilter, hits, pushReach);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                // Only side contacts: a box the player stands on or bumps from below is not pushed.
                bool facesPlayer = hit.normal.x * FacingDirection < -0.7f;
                if (facesPlayer && hit.rigidbody != null && hit.rigidbody.TryGetComponent(out PushableBox box))
                    return box;
            }
            return null;
        }
    }
}
