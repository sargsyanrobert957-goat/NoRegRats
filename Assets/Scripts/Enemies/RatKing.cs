using System;
using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The boss. Waits until BossArena starts the fight, then stomps slowly toward the hero and every
    /// few seconds stops, winds up (a visible squash) and throws a piece of trash in an arc at them.
    /// Bites on contact through ContactDamage. On defeat he shrinks away and drops the Heart. If the
    /// Heart was already taken earlier in the run (the hero died after the fight), he stays gone.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class RatKing : MonoBehaviour
    {
        [Header("Walking")]
        [SerializeField] float walkSpeed = 1.2f;
        [Tooltip("He stops walking when this close to the hero, horizontally.")]
        [SerializeField] float stopDistance = 0.5f;

        [Header("Throwing")]
        [SerializeField] TrashProjectile trashPrefab;
        [Tooltip("Seconds between throws.")]
        [SerializeField] float throwInterval = 2f;
        [Tooltip("Seconds he stands still, squashed, before each throw, so the hero can see it coming.")]
        [SerializeField] float windUpTime = 0.4f;
        [Tooltip("Where the trash leaves his hand, from his feet. x points the way he faces.")]
        [SerializeField] Vector2 throwOrigin = new Vector2(0.8f, 2.4f);
        [Tooltip("Shortest and longest time the trash is in the air; longer for far throws, which also means higher arcs.")]
        [SerializeField] Vector2 flightTime = new Vector2(0.9f, 1.4f);
        [Tooltip("Throw distance covered per second of flight before the time is clamped; lower = higher, slower arcs.")]
        [SerializeField] float throwPace = 6f;

        [Header("Defeat")]
        [SerializeField] HeartPickup heartPrefab;
        [Tooltip("Where the Heart appears, from his feet.")]
        [SerializeField] Vector2 heartOffset = new Vector2(0f, 1.2f);
        [Tooltip("Seconds the defeat poof takes.")]
        [SerializeField] float defeatTime = 0.5f;

        [Header("Look")]
        [Tooltip("Child holding the sprite. Mirrored to face the hero.")]
        [SerializeField] Transform visual;
        [Tooltip("Tick if the drawing faces right; the sketch faces left.")]
        [SerializeField] bool artFacesRight;
        [SerializeField] float waddleBob = 0.08f;
        [SerializeField] float waddleTilt = 4f;
        [SerializeField] float waddleSpeed = 6f;
        [Tooltip("Scale while winding up a throw.")]
        [SerializeField] Vector2 windUpScale = new Vector2(1.08f, 0.9f);

        public Health Health => health;
        public bool IsFighting { get; private set; }
        /// <summary>Raised once he has been beaten and the Heart has dropped.</summary>
        public event Action Defeated;

        Rigidbody2D body;
        Collider2D bodyCollider;
        Health health;
        SpriteRenderer spriteRenderer;
        Transform player;
        Vector3 visualPosition;
        Vector3 visualScale;
        float throwTimer;
        float windUpTimer;
        float waddlePhase;
        int facing = -1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            health = GetComponent<Health>();
            spriteRenderer = visual.GetComponent<SpriteRenderer>();
            visualPosition = visual.localPosition;
            visualScale = visual.localScale;
        }

        void Start()
        {
            if (GameSession.HasHeart)
            {
                gameObject.SetActive(false);
                return;
            }
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement == null)
                return;
            player = movement.transform;
            foreach (Collider2D playerCollider in movement.GetComponents<Collider2D>())
                Physics2D.IgnoreCollision(bodyCollider, playerCollider);
        }

        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        public void StartFight()
        {
            IsFighting = true;
            throwTimer = throwInterval * 0.5f;
        }

        void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = 0f;
            if (IsFighting && !health.IsDead && player != null)
            {
                float dx = player.position.x - body.position.x;
                facing = dx >= 0f ? 1 : -1;
                if (windUpTimer > 0f)
                {
                    windUpTimer -= Time.fixedDeltaTime;
                    if (windUpTimer <= 0f)
                        Throw();
                }
                else
                {
                    throwTimer -= Time.fixedDeltaTime;
                    if (throwTimer <= 0f)
                    {
                        windUpTimer = windUpTime;
                        throwTimer = throwInterval;
                    }
                    else if (Mathf.Abs(dx) > stopDistance)
                    {
                        velocity.x = facing * walkSpeed;
                    }
                }
            }
            body.linearVelocity = velocity;
        }

        void Throw()
        {
            Vector2 origin = body.position + new Vector2(throwOrigin.x * facing, throwOrigin.y);
            Vector2 target = player.position;
            float time = Mathf.Clamp(Mathf.Abs(target.x - origin.x) / throwPace, flightTime.x, flightTime.y);
            Instantiate(trashPrefab).Launch(origin, target, time);
            Sfx.Play(Sfx.Sound.KingThrow);
        }

        void LateUpdate()
        {
            if (health.IsDead)
                return;

            Vector2 scale = Vector2.one;
            float tilt = 0f, bob = 0f;
            if (windUpTimer > 0f)
            {
                scale = windUpScale;
            }
            else if (Mathf.Abs(body.linearVelocity.x) > 0.1f)
            {
                waddlePhase += Time.deltaTime * waddleSpeed;
                float step = Mathf.Sin(waddlePhase);
                bob = Mathf.Abs(step) * waddleBob;
                tilt = step * waddleTilt;
            }
            int mirror = artFacesRight ? facing : -facing;
            visual.localPosition = visualPosition + Vector3.up * bob;
            visual.localRotation = Quaternion.Euler(0f, 0f, tilt);
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * scale.x * mirror, visualScale.y * scale.y, visualScale.z);
        }

        void OnDied()
        {
            IsFighting = false;
            Vector2 center = bodyCollider.bounds.center;
            bodyCollider.enabled = false;
            body.simulated = false;
            Sfx.Play(Sfx.Sound.KingDefeated);
            PuffSpawner.Burst(center, 14, 2.2f, 4f, new Color(0.55f, 0.5f, 0.53f, 0.95f));
            CameraFollow2D.ShakeActive(0.35f, 0.4f);
            StartCoroutine(Defeat());
        }

        IEnumerator Defeat()
        {
            Vector3 startScale = visual.localScale;
            Color startColor = spriteRenderer.color;
            for (float t = 0f; t < defeatTime; t += Time.deltaTime)
            {
                float k = t / defeatTime;
                visual.localScale = Vector3.Lerp(startScale, Vector3.zero, k * k);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - k);
                yield return null;
            }
            Instantiate(heartPrefab, (Vector2)transform.position + heartOffset, Quaternion.identity);
            Defeated?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
