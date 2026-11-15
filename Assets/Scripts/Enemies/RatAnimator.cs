using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Plays a looping set of drawn frames on a rat (the rat's walk and waddle are still done by
    /// RatEnemy), and shows a "punched back" frame for a moment when the rat is hit. Used by the
    /// chain rat (Rat1).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class RatAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [Tooltip("The loop, in order.")]
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 8f;
        [Tooltip("Shown for a moment after the rat is hit.")]
        [SerializeField] Sprite hitFrame;
        [SerializeField] float hitTime = 0.3f;

        Health health;
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

        void Update()
        {
            if (health.IsDead)
                return;
            if (Time.time < hitUntil && hitFrame != null)
            {
                target.sprite = hitFrame;
                return;
            }
            if (frames == null || frames.Length == 0)
                return;
            int index = (int)((Time.time + offset) * framesPerSecond) % frames.Length;
            target.sprite = frames[index];
        }
    }
}
