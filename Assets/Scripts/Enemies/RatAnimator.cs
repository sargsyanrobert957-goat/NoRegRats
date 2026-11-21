using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Shows a rat's drawings (RatEnemy still does the walking): the standing drawing while it walks
    /// (RatEnemy waddles it), whatever frame RatFighter asks for during an attack, and a hit frame
    /// when it is struck. Without a standing drawing it loops its frames instead, as the rats first
    /// did. Used by Rat1 and Rat3.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class RatAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [Tooltip("Shown while walking or standing. Empty: the frames below loop all the time instead.")]
        [SerializeField] Sprite standingFrame;
        [Tooltip("The loop used when there is no standing drawing, in order.")]
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 8f;
        [Tooltip("Shown for a moment after the rat is hit.")]
        [SerializeField] Sprite hitFrame;
        [SerializeField] float hitTime = 0.3f;

        Health health;
        Sprite attackFrame;
        float hitUntil;
        float offset;

        void Awake()
        {
            health = GetComponent<Health>();
            if (target == null)
                target = GetComponentInChildren<SpriteRenderer>();
            // So several rats of this kind don't all animate in step.
            offset = Random.value * 10f;
        }

        void OnEnable() => health.Damaged += OnDamaged;
        void OnDisable() => health.Damaged -= OnDamaged;

        void OnDamaged(int amount, Vector2 source) => hitUntil = Time.time + hitTime;

        /// <summary>The frame of an attack in progress; null goes back to standing.</summary>
        public void ShowAttackFrame(Sprite frame) => attackFrame = frame;

        void Update()
        {
            if (health.IsDead)
                return;
            if (Time.time < hitUntil && hitFrame != null)
                target.sprite = hitFrame;
            else if (attackFrame != null)
                target.sprite = attackFrame;
            else if (standingFrame != null)
                target.sprite = standingFrame;
            else if (frames != null && frames.Length > 0)
                target.sprite = frames[(int)((Time.time + offset) * framesPerSecond) % frames.Length];
        }
    }
}
