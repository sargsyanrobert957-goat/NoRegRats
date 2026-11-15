using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The gun rat (Rat2). It stands still and turns to face the hero. When the hero is in range,
    /// at about its own height and not behind a wall, it raises its gun (a short warning), fires a
    /// bullet straight along the floor and recoils; then it waits before the next shot. A punch
    /// interrupts the shot and staggers it. At 0 HP it shrinks away in a poof. The hero can jump
    /// the bullet. Bites on contact through ContactDamage.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class RatShooter : MonoBehaviour
    {
        [Header("Shooting")]
        [SerializeField] EnemyBullet bulletPrefab;
        [Tooltip("The hero is only shot at inside this horizontal distance.")]
        [SerializeField] float sightRange = 11f;
        [Tooltip("...and when their height differs from this rat's by less than this.")]
        [SerializeField] float sightHeight = 2.5f;
        [Tooltip("Seconds between the end of one shot and the start of the next.")]
        [SerializeField] float fireInterval = 1.8f;
        [Tooltip("Seconds the gun is raised before it fires: the warning.")]
        [SerializeField] float aimTime = 0.5f;
        [Tooltip("Where the bullet leaves the gun, from the rat's feet. x points the way it faces.")]
        [SerializeField] Vector2 muzzleOffset = new Vector2(0.9f, 1f);

        [Header("Look")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite standingFrame;
        [Tooltip("Aim, muzzle flash, recoil, recover. The bullet leaves on the second frame.")]
        [SerializeField] Sprite[] shootFrames;
        [Tooltip("Shown for a moment after the rat is hit.")]
        [SerializeField] Sprite hitFrame;
        [Tooltip("Tick if the drawing faces right; this one faces left.")]
        [SerializeField] bool artFacesRight;
        [Tooltip("Seconds each frame after the aim frame is shown.")]
        [SerializeField] float frameTime = 0.12f;

        [Header("When Hit")]
        [SerializeField] float hitKnockback = 2.5f;
        [Tooltip("Seconds the rat is staggered after being hit (it can't shoot).")]
        [SerializeField] float hitStun = 0.4f;
        [Tooltip("Seconds the death poof takes.")]
        [SerializeField] float deathTime = 0.2f;

        Rigidbody2D body;
        Collider2D bodyCollider;
        Health health;
        Health playerHealth;
        Transform player;
        readonly RaycastHit2D[] sightHits = new RaycastHit2D[8];
        ContactFilter2D solidFilter;
        Coroutine shootRoutine;
        int facing = -1;
        float nextShot;
        float hitUntil;
        bool shooting;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            health = GetComponent<Health>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        void Start()
        {
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement == null)
                return;
            player = movement.transform;
            playerHealth = movement.GetComponent<Health>();
            // Rats and the hero pass through each other; the bite is ContactDamage's job.
            foreach (Collider2D playerCollider in movement.GetComponents<Collider2D>())
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
            // Stands its ground; only a hit pushes it, and the push dies away.
            Vector2 velocity = body.linearVelocity;
            velocity.x = Time.time < hitUntil ? velocity.x * 0.9f : 0f;
            body.linearVelocity = velocity;
        }

        void Update()
        {
            if (health.IsDead || player == null)
                return;

            if (Time.time < hitUntil)
            {
                Show(hitFrame);
                return;
            }
            if (shooting)
                return;

            FacePlayer();
            Show(standingFrame);
            if (Time.time >= nextShot && CanSeePlayer())
                shootRoutine = StartCoroutine(Shoot());
        }

        void FacePlayer()
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f)
                facing = dx > 0f ? 1 : -1;
            spriteRenderer.flipX = artFacesRight ? facing < 0 : facing > 0;
        }

        void Show(Sprite sprite)
        {
            if (sprite != null)
                spriteRenderer.sprite = sprite;
        }

        bool CanSeePlayer()
        {
            if (playerHealth != null && playerHealth.IsDead)
                return false;
            Vector2 toPlayer = player.position - transform.position;
            if (Mathf.Abs(toPlayer.x) > sightRange || Mathf.Abs(toPlayer.y) > sightHeight)
                return false;

            // Only a wall or platform blocks the view; rats and the hero don't.
            Vector2 from = (Vector2)transform.position + new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
            Vector2 to = player.position + Vector3.up * muzzleOffset.y;
            int count = Physics2D.Linecast(from, to, solidFilter, sightHits);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D hitBody = sightHits[i].rigidbody;
                if (hitBody == null)
                    return false;
            }
            return true;
        }

        IEnumerator Shoot()
        {
            shooting = true;
            Show(FrameAt(0));
            yield return new WaitForSeconds(aimTime);

            // Fire on the second frame.
            Vector2 muzzle = (Vector2)transform.position + new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
            if (bulletPrefab != null)
            {
                EnemyBullet bullet = Instantiate(bulletPrefab);
                bullet.Launch(muzzle, facing);
            }
            Sfx.Play(Sfx.Sound.Gunshot);
            PuffSpawner.Burst(muzzle, 3, 0.4f, 2f, new Color(1f, 0.9f, 0.5f, 0.9f));

            for (int i = 1; shootFrames != null && i < shootFrames.Length; i++)
            {
                Show(shootFrames[i]);
                yield return new WaitForSeconds(frameTime);
            }

            shooting = false;
            nextShot = Time.time + fireInterval;
        }

        Sprite FrameAt(int index) =>
            shootFrames != null && index < shootFrames.Length ? shootFrames[index] : standingFrame;

        void OnDamaged(int amount, Vector2 source)
        {
            // A hit cancels the shot in progress.
            if (shootRoutine != null)
                StopCoroutine(shootRoutine);
            shooting = false;
            hitUntil = Time.time + hitStun;
            nextShot = Time.time + hitStun + fireInterval * 0.5f;
            float away = Mathf.Sign(body.position.x - source.x);
            body.linearVelocity = new Vector2(away * hitKnockback, body.linearVelocity.y);
        }

        void OnDied()
        {
            if (shootRoutine != null)
                StopCoroutine(shootRoutine);
            shooting = false;
            bodyCollider.enabled = false;
            body.simulated = false;
            Sfx.Play(Sfx.Sound.RatPoof);
            PuffSpawner.Burst(bodyCollider.bounds.center, 7, 1.1f, 3f, new Color(0.55f, 0.5f, 0.53f, 0.95f));
            StartCoroutine(Poof());
        }

        // Shrinks and fades the rat away, then switches it off.
        IEnumerator Poof()
        {
            Transform visual = spriteRenderer.transform;
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
