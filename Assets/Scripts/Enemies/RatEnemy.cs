using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A rat that patrols back and forth, turning at walls, ledges and the end of its patrol range.
    /// When shot it is knocked back and stunned for a moment; at 0 HP it shrinks away in a poof and
    /// deactivates. The single drawn frame is brought to life with a waddle (bob and tilt). Rats pass
    /// through the hero; ContactDamage on the same object does the biting. Different rat types are
    /// the same prefab setup with different Health, ContactDamage and speed values.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class RatEnemy : MonoBehaviour
    {
        [Header("Patrol")]
        [SerializeField] float walkSpeed = 1.6f;
        [Tooltip("How far the rat may wander from where it starts, each way. 0 = until a wall or ledge.")]
        [SerializeField] float patrolRange;
        [Tooltip("Direction it starts walking: 1 = right, -1 = left.")]
        [SerializeField] int startDirection = -1;

        [Header("When Hit")]
        [Tooltip("Speed the rat is pushed away from the shot.")]
        [SerializeField] float hitKnockback = 3f;
        [Tooltip("Seconds the rat stops patrolling after being hit.")]
        [SerializeField] float hitStun = 0.25f;
        [Tooltip("Seconds the death poof takes.")]
        [SerializeField] float deathTime = 0.2f;

        [Header("Waddle")]
        [Tooltip("Child holding the sprite. It is mirrored to face the walking direction.")]
        [SerializeField] Transform visual;
        [Tooltip("How high the rat bobs on each step, in world units.")]
        [SerializeField] float waddleBob = 0.06f;
        [Tooltip("How far the rat tilts side to side, in degrees.")]
        [SerializeField] float waddleTilt = 6f;
        [Tooltip("Steps per second, times π.")]
        [SerializeField] float waddleSpeed = 10f;

        Rigidbody2D body;
        Collider2D bodyCollider;
        Health health;
        SpriteRenderer spriteRenderer;
        ContactFilter2D solidFilter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[4];
        Vector3 visualPosition;
        Vector3 visualScale;
        Vector2 home;
        int direction;
        float stunTimer;
        float waddlePhase;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            health = GetComponent<Health>();
            spriteRenderer = visual.GetComponent<SpriteRenderer>();
            visualPosition = visual.localPosition;
            visualScale = visual.localScale;
            home = body.position;
            direction = startDirection >= 0 ? 1 : -1;
        }

        void Start()
        {
            // Rats and the hero pass through each other; the bite is ContactDamage's job.
            var player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                foreach (Collider2D playerCollider in player.GetComponents<Collider2D>())
                    Physics2D.IgnoreCollision(bodyCollider, playerCollider);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void FixedUpdate()
        {
            if (health.IsDead)
                return;

            Vector2 velocity = body.linearVelocity;
            if (stunTimer > 0f)
            {
                stunTimer -= Time.fixedDeltaTime;
            }
            else
            {
                if (ShouldTurn())
                    direction = -direction;
                velocity.x = direction * walkSpeed;
            }
            body.linearVelocity = velocity;
        }

        void LateUpdate()
        {
            if (health.IsDead)
                return;

            // Waddle only while walking; settle back to upright when stopped.
            bool walking = Mathf.Abs(body.linearVelocity.x) > 0.1f;
            waddlePhase = walking ? waddlePhase + Time.deltaTime * waddleSpeed : 0f;
            float step = Mathf.Sin(waddlePhase);
            visual.localPosition = visualPosition + Vector3.up * (Mathf.Abs(step) * waddleBob);
            visual.localRotation = Quaternion.Euler(0f, 0f, step * waddleTilt);
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * direction, visualScale.y, visualScale.z);
        }

        bool ShouldTurn()
        {
            if (patrolRange > 0f && (body.position.x - home.x) * direction >= patrolRange)
                return true;

            // A wall, crate or other rat right ahead (the hero doesn't count).
            int count = body.Cast(new Vector2(direction, 0f), solidFilter, hits, 0.05f);
            for (int i = 0; i < count; i++)
            {
                bool facesUs = hits[i].normal.x * direction < -0.5f;
                bool isPlayer = hits[i].rigidbody != null && hits[i].rigidbody.TryGetComponent(out PlayerMovement _);
                if (facesUs && !isPlayer)
                    return true;
            }

            // No ground just past the front foot: a ledge.
            Bounds bounds = bodyCollider.bounds;
            var frontFoot = new Vector2(direction > 0 ? bounds.max.x + 0.05f : bounds.min.x - 0.05f, bounds.min.y + 0.05f);
            return Physics2D.Raycast(frontFoot, Vector2.down, solidFilter, hits, 0.3f) == 0;
        }

        void OnDamaged(int amount, Vector2 source)
        {
            stunTimer = hitStun;
            float away = Mathf.Sign(body.position.x - source.x);
            body.linearVelocity = new Vector2(away * hitKnockback, body.linearVelocity.y);
        }

        void OnDied()
        {
            bodyCollider.enabled = false;
            body.simulated = false;
            StartCoroutine(Poof());
        }

        // Shrinks and fades the rat away, then switches it off.
        IEnumerator Poof()
        {
            Vector3 startScale = visual.localScale;
            Color startColor = spriteRenderer.color;
            for (float t = 0f; t < deathTime; t += Time.deltaTime)
            {
                float k = t / deathTime;
                visual.localScale = Vector3.Lerp(startScale, Vector3.zero, k * k);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - k);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
