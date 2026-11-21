using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The gun rat (Rat2). It faces the hero; in range, at its own height and with a clear line, it
    /// raises its gun (the warning) and fires, then waits a varying time. Each attack is picked at
    /// random: a single low shot (jump it), a burst of three, or a high shot with the gun tilted up
    /// (crouch under it). It backs away when the hero comes close. A punch interrupts the shot. It
    /// bites on contact through ContactDamage.
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
        [Tooltip("The wait between shots varies by up to this much either way, as a fraction (0.3 = 30%).")]
        [SerializeField] float intervalJitter = 0.3f;
        [Tooltip("Seconds the gun is raised before it fires: the warning.")]
        [SerializeField] float aimTime = 0.5f;
        [Tooltip("Where the bullet leaves the gun, from the rat's feet. x points the way it faces.")]
        [SerializeField] Vector2 muzzleOffset = new Vector2(0.9f, 1f);

        [Header("Patterns")]
        [Tooltip("How often a single shot is picked, compared with the others.")]
        [SerializeField] float singleWeight = 3f;
        [Tooltip("How often a burst is picked.")]
        [SerializeField] float burstWeight = 2f;
        [Tooltip("How often a high shot is picked.")]
        [SerializeField] float highWeight = 2f;
        [Tooltip("Shots in a burst.")]
        [SerializeField] int burstCount = 3;
        [Tooltip("Seconds between the shots of a burst.")]
        [SerializeField] float burstGap = 0.16f;
        [Tooltip("How much higher a high shot leaves the gun: over a crouching hero, into a standing one.")]
        [SerializeField] float highShotLift = 0.5f;
        [Tooltip("How far the drawing tilts up while aiming a high shot, in degrees (the warning).")]
        [SerializeField] float highShotTilt = 14f;

        [Header("Backing Off")]
        [Tooltip("Backs away while the hero is closer than this, horizontally.")]
        [SerializeField] float backOffRange = 2.5f;
        [SerializeField] float backOffSpeed = 1.4f;

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
        readonly RaycastHit2D[] blockHits = new RaycastHit2D[4];
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
            // A hit's push dies away; otherwise it stands, or backs off from a hero who gets close.
            Vector2 velocity = body.linearVelocity;
            if (Time.time < hitUntil)
                velocity.x *= 0.9f;
            else if (!shooting && TooClose() && !Blocked(-facing))
                velocity.x = -facing * backOffSpeed;
            else
                velocity.x = 0f;
            body.linearVelocity = velocity;
        }

        bool TooClose() =>
            player != null && (playerHealth == null || !playerHealth.IsDead)
            && Mathf.Abs(player.position.x - transform.position.x) < backOffRange
            && Mathf.Abs(player.position.y - transform.position.y) < sightHeight;

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
            // Too close, it backs off first; cornered, it shoots anyway.
            bool makingRoom = TooClose() && !Blocked(-facing);
            if (Time.time >= nextShot && !makingRoom && CanSeePlayer())
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
            float roll = Random.value * (singleWeight + burstWeight + highWeight);
            bool high = roll >= singleWeight + burstWeight;
            int shots = !high && roll >= singleWeight ? Mathf.Max(1, burstCount) : 1;

            // The aim is the warning; for a high shot the gun tilts up.
            Show(FrameAt(0));
            Transform visual = spriteRenderer.transform;
            visual.localRotation = Quaternion.Euler(0f, 0f, high ? highShotTilt * facing : 0f);
            yield return new WaitForSeconds(aimTime);

            for (int shot = 0; shot < shots; shot++)
            {
                if (shot > 0)
                {
                    Show(FrameAt(0));
                    yield return new WaitForSeconds(burstGap);
                }
                Fire(high);
                if (shot < shots - 1)
                {
                    yield return new WaitForSeconds(frameTime * 0.5f);
                    continue;
                }
                // The rest of the shooting frames play once, after the last shot.
                for (int i = 1; shootFrames != null && i < shootFrames.Length; i++)
                {
                    Show(shootFrames[i]);
                    yield return new WaitForSeconds(frameTime);
                }
            }

            visual.localRotation = Quaternion.identity;
            shooting = false;
            nextShot = Time.time + fireInterval * Random.Range(1f - intervalJitter, 1f + intervalJitter);
        }

        // Fires on the second frame (the muzzle flash).
        void Fire(bool high)
        {
            Show(FrameAt(1));
            Vector2 muzzle = (Vector2)transform.position + new Vector2(muzzleOffset.x * facing, muzzleOffset.y + (high ? highShotLift : 0f));
            if (bulletPrefab != null)
            {
                EnemyBullet bullet = Instantiate(bulletPrefab);
                bullet.Launch(muzzle, facing);
            }
            Sfx.Play(Sfx.Sound.Gunshot);
            PuffSpawner.Burst(muzzle, 3, 0.4f, 2f, new Color(1f, 0.9f, 0.5f, 0.9f));
        }

        Sprite FrameAt(int index) =>
            shootFrames != null && index < shootFrames.Length ? shootFrames[index] : standingFrame;

        // A wall or crate that way, or a ledge, so backing off never walks it off an edge.
        bool Blocked(int towards)
        {
            int count = body.Cast(new Vector2(towards, 0f), solidFilter, blockHits, 0.05f);
            for (int i = 0; i < count; i++)
            {
                bool facesUs = blockHits[i].normal.x * towards < -0.5f;
                bool isPlayer = blockHits[i].rigidbody != null && blockHits[i].rigidbody.TryGetComponent(out PlayerMovement _);
                if (facesUs && !isPlayer)
                    return true;
            }
            Bounds bounds = bodyCollider.bounds;
            var foot = new Vector2(towards > 0 ? bounds.max.x + 0.05f : bounds.min.x - 0.05f, bounds.min.y + 0.05f);
            return Physics2D.Raycast(foot, Vector2.down, solidFilter, blockHits, 0.3f) == 0;
        }

        void OnDamaged(int amount, Vector2 source)
        {
            // A hit cancels the shot in progress.
            if (shootRoutine != null)
                StopCoroutine(shootRoutine);
            shooting = false;
            spriteRenderer.transform.localRotation = Quaternion.identity;
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
