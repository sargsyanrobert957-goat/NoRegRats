using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A patrolling rat that turns at walls, ledges and the end of its range. A hit knocks it back and
    /// stuns it; at 0 HP it shrinks away. ContactDamage does the biting. Rat types are prefab
    /// variations of Health, damage and speed.
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
        // Where it last turned at a wall or ledge, and which side that was on (0 = not yet).
        int wallSide;
        float wallX;
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
                velocity.x = Patrol() ? direction * walkSpeed : 0f;
            }
            body.linearVelocity = velocity;
        }

        // Turns at walls, ledges and the end of the range; false when it should stand still.
        bool Patrol()
        {
            float x = body.position.x;
            if (Blocked(direction))
            {
                // Walled or ledged in on both sides: stand, facing the same way, rather than turn
                // round every step.
                if (Blocked(-direction))
                    return false;
                wallSide = direction;
                wallX = x;
                direction = -direction;
            }
            else if (patrolRange > 0f && (x - home.x) * direction >= patrolRange)
            {
                // Out of range, but the wall or ledge it last turned at stands between it and home
                // (knocked off its perch, say): this spot becomes home, or the range and the wall
                // would turn it back and forth for ever.
                int homeward = -direction;
                if (wallSide == homeward && Mathf.Abs(wallX - x) < Mathf.Abs(home.x - x))
                    home = body.position;
                else
                    direction = homeward;
            }
            return true;
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

        // A wall, crate or other rat right ahead in that direction (the hero doesn't count), or a ledge.
        bool Blocked(int towards)
        {
            int count = body.Cast(new Vector2(towards, 0f), solidFilter, hits, 0.05f);
            for (int i = 0; i < count; i++)
            {
                bool facesUs = hits[i].normal.x * towards < -0.5f;
                bool isPlayer = hits[i].rigidbody != null && hits[i].rigidbody.TryGetComponent(out PlayerMovement _);
                if (facesUs && !isPlayer)
                    return true;
            }

            // No ground just past the front foot: a ledge.
            Bounds bounds = bodyCollider.bounds;
            var frontFoot = new Vector2(towards > 0 ? bounds.max.x + 0.05f : bounds.min.x - 0.05f, bounds.min.y + 0.05f);
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
            Sfx.Play(Sfx.Sound.RatPoof);
            PuffSpawner.Burst(bodyCollider.bounds.center, 7, 1.1f, 3f, new Color(0.55f, 0.5f, 0.53f, 0.95f));
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
