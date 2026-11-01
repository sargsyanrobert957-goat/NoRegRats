using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Feedback for getting hit: tints the sprite for a moment, and optionally blinks it for as long
    /// as the Health stays invulnerable afterwards (used on the hero, who also blinks once dead).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] Color flashColor = new Color(1f, 0.35f, 0.35f);
        [Tooltip("Seconds the tint lasts after a hit.")]
        [SerializeField] float flashTime = 0.12f;
        [Tooltip("Blink the sprite while the Health is invulnerable after a hit.")]
        [SerializeField] bool blinkWhileInvulnerable;
        [Tooltip("Blinks per second.")]
        [SerializeField] float blinkRate = 12f;
        [Tooltip("Sprite opacity during the faded half of a blink.")]
        [SerializeField, Range(0f, 1f)] float blinkAlpha = 0.35f;

        Health health;
        float flashUntil;

        void Awake()
        {
            health = GetComponent<Health>();
            if (target == null)
                target = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable() => health.Damaged += OnDamaged;
        void OnDisable() => health.Damaged -= OnDamaged;

        void OnDamaged(int amount, Vector2 source) => flashUntil = Time.time + flashTime;

        void LateUpdate()
        {
            // A death effect may be fading the sprite out; leave its colour alone. Only things that
            // don't blink have one: the hero keeps blinking until the level restarts.
            if (health.IsDead && !blinkWhileInvulnerable)
                return;
            Color color = Time.time < flashUntil ? flashColor : Color.white;
            bool blinking = blinkWhileInvulnerable && (health.IsInvulnerable || health.IsDead);
            if (blinking && Mathf.Repeat(Time.time * blinkRate, 1f) < 0.5f)
                color.a = blinkAlpha;
            target.color = color;
        }
    }
}
