using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Shows the hero's drawn frames and fakes the animations that aren't drawn yet with squash and
    /// stretch: the run cycle plays in step with ground speed, the standing frame breathes, the air
    /// uses one run frame stretched while rising and squashed while falling, take-off and landing
    /// get a quick squash/stretch that springs back, crouching squashes the figure and pushing leans
    /// it forward. Faces the player's FacingDirection by mirroring this object.
    /// Put it on the "Visual" child, positioned at the bottom of the player's collider, with sprites
    /// whose pivot is at the feet so all scaling keeps the feet planted.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroAnimator : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;

        [Header("Frames")]
        [SerializeField] Sprite idleFrame;
        [Tooltip("The run cycle, in order.")]
        [SerializeField] Sprite[] runFrames;
        [Tooltip("Index of the run frame shown in the air (one with both feet off the ground).")]
        [SerializeField] int airFrame;
        [Tooltip("Ground distance covered by one full run cycle. Raise it if the feet look like they slide.")]
        [SerializeField] float runCycleDistance = 3f;
        [Tooltip("Below this ground speed the hero counts as standing still.")]
        [SerializeField] float moveThreshold = 0.2f;

        [Header("Idle Breathing")]
        [Tooltip("How much taller the figure gets at the top of a breath (0.015 = 1.5%).")]
        [SerializeField] float breathAmount = 0.015f;
        [Tooltip("Breaths per second, times 2π.")]
        [SerializeField] float breathSpeed = 2.5f;

        [Header("Squash and Stretch")]
        [Tooltip("Scale while crouching. The crouched collider is about two thirds of the standing one.")]
        [SerializeField] Vector2 crouchScale = new Vector2(1.12f, 0.66f);
        [SerializeField] Vector2 riseScale = new Vector2(0.94f, 1.08f);
        [SerializeField] Vector2 fallScale = new Vector2(1.03f, 0.97f);
        [Tooltip("Scale snapped to on take-off, then eased back.")]
        [SerializeField] Vector2 takeOffScale = new Vector2(0.85f, 1.2f);
        [Tooltip("Scale snapped to on landing, then eased back.")]
        [SerializeField] Vector2 landScale = new Vector2(1.2f, 0.8f);
        [Tooltip("How quickly the scale eases towards its target. Higher is snappier.")]
        [SerializeField] float scaleSpring = 14f;

        [Header("Pushing")]
        [Tooltip("Extra forward lean while pushing a box, in degrees.")]
        [SerializeField] float pushLean = 6f;
        [Tooltip("How quickly the lean blends in and out, in degrees per second.")]
        [SerializeField] float leanSpeed = 60f;

        SpriteRenderer spriteRenderer;
        Vector2 scale = Vector2.one;
        float runPhase;
        float lean;
        bool wasGrounded = true;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (movement == null)
                movement = GetComponentInParent<PlayerMovement>();
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector2 velocity = movement.Velocity;
            float speed = Mathf.Abs(velocity.x);
            bool grounded = movement.IsGrounded;

            // Take-off and landing snap the scale, which then springs back towards the target below.
            if (grounded && !wasGrounded)
                scale = landScale;
            else if (!grounded && wasGrounded && velocity.y > 0.1f)
                scale = takeOffScale;
            wasGrounded = grounded;

            Vector2 target;
            if (!grounded)
            {
                spriteRenderer.sprite = runFrames[airFrame];
                target = velocity.y > 0f ? riseScale : fallScale;
            }
            else if (speed > moveThreshold)
            {
                // Advance by distance travelled, so the cycle speeds up and slows down with the hero.
                runPhase = Mathf.Repeat(runPhase + speed * dt / runCycleDistance, 1f);
                spriteRenderer.sprite = runFrames[(int)(runPhase * runFrames.Length) % runFrames.Length];
                target = Vector2.one;
            }
            else
            {
                runPhase = 0f;
                spriteRenderer.sprite = idleFrame;
                float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
                target = new Vector2(1f - breath * 0.5f, 1f + breath);
            }
            if (movement.IsCrouching)
                target = Vector2.Scale(target, crouchScale);

            scale = Vector2.Lerp(scale, target, 1f - Mathf.Exp(-scaleSpring * dt));
            lean = Mathf.MoveTowards(lean, movement.IsPushing ? pushLean : 0f, leanSpeed * dt);

            int facing = movement.FacingDirection;
            transform.localScale = new Vector3(scale.x * facing, scale.y, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, -lean * facing);
        }
    }
}
