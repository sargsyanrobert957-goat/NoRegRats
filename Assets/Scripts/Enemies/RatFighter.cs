using System;
using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A rat's attacks (Rat1's claw swipe, Rat3's machete slash). When the hero comes near at its
    /// height it stops patrolling, faces them, closes in and attacks; between attacks it rests. Each
    /// attack is picked at random from a weighted list (a single strike, two in a row, a lunge that
    /// closes the gap, a feint that baits a punch), and each starts with a wind-up (the rat crouches
    /// and flashes) so it can be read. A punch interrupts it. RatEnemy hands it the walking while it
    /// fights; ContactDamage still bites.
    /// </summary>
    [RequireComponent(typeof(RatEnemy), typeof(Health))]
    [DefaultExecutionOrder(100)]
    public class RatFighter : MonoBehaviour
    {
        public enum Move { Strike, DoubleStrike, LungeStrike, Feint }

        [Serializable]
        public struct Choice
        {
            public Move move;
            [Tooltip("How often it is picked compared with the others.")]
            public float weight;
        }

        [Header("Noticing")]
        [Tooltip("The hero is fought within this horizontal distance...")]
        [SerializeField] float noticeRange = 3.5f;
        [Tooltip("...and when their height differs from this rat's by less than this.")]
        [SerializeField] float noticeHeight = 1.3f;
        [Tooltip("Speed while closing in on the hero.")]
        [SerializeField] float approachSpeed = 2.2f;

        [Header("Strike")]
        [Tooltip("How far the strike reaches past the front of the rat's collider, in world units.")]
        [SerializeField] float reach = 1.6f;
        [Tooltip("Height of the strike's hit box, from the rat's feet.")]
        [SerializeField] float strikeHeight = 1.3f;
        [Tooltip("Hearts a strike takes.")]
        [SerializeField] int damage = 2;
        [Tooltip("Seconds of wind-up before a strike: the warning.")]
        [SerializeField] float windUpTime = 0.45f;
        [Tooltip("Wind-up before the second strike of a double, shorter.")]
        [SerializeField] float secondWindUpTime = 0.2f;
        [Tooltip("Seconds the strike frames take.")]
        [SerializeField] float strikeTime = 0.2f;
        [Tooltip("Seconds it stands still after striking.")]
        [SerializeField] float recoverTime = 0.35f;
        [Tooltip("Speed of the lunge before a lunging strike.")]
        [SerializeField] float lungeSpeed = 5.5f;
        [Tooltip("Seconds the lunge lasts.")]
        [SerializeField] float lungeTime = 0.25f;
        [Tooltip("Seconds of rest between attacks, picked at random between x and y.")]
        [SerializeField] Vector2 restTime = new Vector2(0.8f, 1.5f);
        [Tooltip("The attacks and how often each is picked.")]
        [SerializeField] Choice[] moves =
        {
            new Choice { move = Move.Strike, weight = 3f },
            new Choice { move = Move.DoubleStrike, weight = 2f },
            new Choice { move = Move.LungeStrike, weight = 2f },
            new Choice { move = Move.Feint, weight = 1f },
        };

        [Header("Look")]
        [SerializeField] RatAnimator ratAnimator;
        [Tooltip("Drawings the wind-up steps through; the last is held until the strike.")]
        [SerializeField] Sprite[] windUpFrames;
        [Tooltip("Drawings of the strike itself; it hurts on the first.")]
        [SerializeField] Sprite[] strikeFrames;
        [Tooltip("Scale at the bottom of the wind-up: lower and wider, about to spring.")]
        [SerializeField] Vector2 windUpSquash = new Vector2(1.1f, 0.88f);
        [Tooltip("The warning tint it flashes during the wind-up.")]
        [SerializeField] Color warningTint = new Color(1f, 0.82f, 0.45f);
        [Tooltip("Flashes per second during the wind-up.")]
        [SerializeField] float flashRate = 9f;

        RatEnemy rat;
        Rigidbody2D body;
        Collider2D bodyCollider;
        Health health;
        SpriteRenderer spriteRenderer;
        Transform player;
        Health playerHealth;
        Collider2D playerCollider;
        Coroutine attack;
        float attackVelocity;
        float restUntil;
        bool warning;
        bool tinted;
        Color tintedColor;
        Color untintedColor;
        Move? planned;

        /// <summary>Squash and stretch for the rat's drawing (RatEnemy applies it).</summary>
        public Vector2 Pose { get; private set; } = Vector2.one;

        public bool Attacking => attack != null;

        /// <summary>The attack in progress, if any.</summary>
        public Move? CurrentMove { get; private set; }

        void Awake()
        {
            rat = GetComponent<RatEnemy>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            health = GetComponent<Health>();
            if (ratAnimator == null)
                ratAnimator = GetComponent<RatAnimator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        void Start()
        {
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement == null)
                return;
            player = movement.transform;
            playerHealth = movement.GetComponent<Health>();
            playerCollider = movement.GetComponent<Collider2D>();
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

        /// <summary>
        /// Called by RatEnemy each physics step. Returns false when the rat should just patrol;
        /// otherwise sets how fast it moves and which way it faces.
        /// </summary>
        public bool Steer(ref float velocityX, ref int direction)
        {
            if (Attacking)
            {
                velocityX = attackVelocity;
                return true;
            }
            if (player == null || playerHealth.IsDead)
                return false;

            float dx = player.position.x - transform.position.x;
            float dy = playerCollider.bounds.min.y - bodyCollider.bounds.min.y;
            if (Mathf.Abs(dx) > noticeRange || Mathf.Abs(dy) > noticeHeight)
            {
                planned = null;
                return false;
            }

            direction = dx >= 0f ? 1 : -1;
            velocityX = 0f;
            if (Time.time < restUntil)
                return true;

            planned ??= Pick();
            float gap = Gap(direction);
            float lungeReach = planned == Move.LungeStrike ? lungeSpeed * lungeTime : 0f;
            if (gap > reach * 0.85f + lungeReach)
            {
                // Close in, but never off a ledge or into a wall.
                if (!rat.Blocked(direction))
                    velocityX = direction * approachSpeed;
                return true;
            }
            attack = StartCoroutine(Attack(planned.Value, direction));
            planned = null;
            return true;
        }

        // Distance between the rat's front and the hero's near side (negative when overlapping).
        float Gap(int direction)
        {
            Bounds mine = bodyCollider.bounds;
            Bounds theirs = playerCollider.bounds;
            return direction > 0 ? theirs.min.x - mine.max.x : mine.min.x - theirs.max.x;
        }

        Move Pick()
        {
            float total = 0f;
            foreach (Choice choice in moves)
                total += Mathf.Max(0f, choice.weight);
            float roll = UnityEngine.Random.value * total;
            foreach (Choice choice in moves)
            {
                roll -= Mathf.Max(0f, choice.weight);
                if (roll <= 0f)
                    return choice.move;
            }
            return Move.Strike;
        }

        IEnumerator Attack(Move move, int direction)
        {
            CurrentMove = move;
            attackVelocity = 0f;
            yield return WindUp(windUpTime);

            if (move == Move.Feint)
            {
                // Holds the threat a beat, then lets it drop: a punch thrown at it now comes too early.
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                if (move == Move.LungeStrike)
                {
                    for (float t = 0f; t < lungeTime; t += Time.fixedDeltaTime)
                    {
                        attackVelocity = rat.Blocked(direction) ? 0f : direction * lungeSpeed;
                        yield return new WaitForFixedUpdate();
                    }
                    attackVelocity = 0f;
                }
                yield return Strike(direction);
                if (move == Move.DoubleStrike)
                {
                    yield return WindUp(secondWindUpTime);
                    yield return Strike(direction);
                }
            }

            ratAnimator.ShowAttackFrame(null);
            yield return new WaitForSeconds(recoverTime);
            Finish(UnityEngine.Random.Range(restTime.x, restTime.y));
        }

        IEnumerator WindUp(float time)
        {
            warning = true;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                int frame = Mathf.Min(windUpFrames.Length - 1, (int)(k * windUpFrames.Length));
                if (windUpFrames.Length > 0)
                    ratAnimator.ShowAttackFrame(windUpFrames[frame]);
                Pose = Vector2.Lerp(Vector2.one, windUpSquash, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }
            warning = false;
        }

        IEnumerator Strike(int direction)
        {
            // Springs out of the crouch, stretched a little the other way.
            Pose = new Vector2(1f / windUpSquash.x, 1f / windUpSquash.y);
            Sfx.Play(Sfx.Sound.Punch);
            HitHero(direction);
            float frameTime = strikeTime / Mathf.Max(1, strikeFrames.Length);
            foreach (Sprite frame in strikeFrames)
            {
                ratAnimator.ShowAttackFrame(frame);
                for (float t = 0f; t < frameTime; t += Time.deltaTime)
                {
                    Pose = Vector2.Lerp(Pose, Vector2.one, 0.3f);
                    yield return null;
                }
            }
            Pose = Vector2.one;
        }

        void HitHero(int direction)
        {
            if (playerHealth == null || playerHealth.IsDead)
                return;
            Bounds mine = bodyCollider.bounds;
            float front = direction > 0 ? mine.max.x : mine.min.x;
            // From a little inside the rat (so a hero right on top of it is hit too) to the strike's reach.
            float near = front - direction * mine.extents.x;
            float far = front + direction * reach;
            Bounds theirs = playerCollider.bounds;
            bool across = Mathf.Max(near, far) >= theirs.min.x && Mathf.Min(near, far) <= theirs.max.x;
            bool level = theirs.min.y <= mine.min.y + strikeHeight && theirs.max.y >= mine.min.y;
            if (across && level)
                playerHealth.TakeDamage(damage, mine.center);
        }

        void Finish(float rest)
        {
            attack = null;
            CurrentMove = null;
            attackVelocity = 0f;
            warning = false;
            Pose = Vector2.one;
            ratAnimator.ShowAttackFrame(null);
            restUntil = Time.time + rest;
        }

        // A punch knocks it out of its attack; it needs a moment before the next one.
        void OnDamaged(int amount, Vector2 source)
        {
            if (attack != null)
                StopCoroutine(attack);
            Finish(UnityEngine.Random.Range(restTime.x, restTime.y) * 0.6f);
            planned = null;
        }

        void OnDied()
        {
            if (attack != null)
                StopCoroutine(attack);
            attack = null;
            CurrentMove = null;
            Pose = Vector2.one;
            ratAnimator.ShowAttackFrame(null);
            warning = false;
        }

        // After DamageFlash (it sets the colour every frame), so the warning flash shows on top.
        void LateUpdate()
        {
            if (spriteRenderer == null)
                return;
            // Undo last frame's tint if nothing else has reset the colour since.
            if (tinted && spriteRenderer.color == tintedColor)
                spriteRenderer.color = untintedColor;
            tinted = false;
            if (!warning || Mathf.Repeat(Time.time * flashRate, 1f) >= 0.5f)
                return;
            untintedColor = spriteRenderer.color;
            tintedColor = untintedColor * warningTint;
            spriteRenderer.color = tintedColor;
            tinted = true;
        }

        void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col == null)
                return;
            Bounds b = col.bounds;
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireCube(new Vector3(b.min.x - reach * 0.5f, b.min.y + strikeHeight * 0.5f), new Vector3(reach, strikeHeight, 0f));
            Gizmos.DrawWireCube(new Vector3(b.max.x + reach * 0.5f, b.min.y + strikeHeight * 0.5f), new Vector3(reach, strikeHeight, 0f));
        }
    }
}
