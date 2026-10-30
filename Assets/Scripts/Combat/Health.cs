using System;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Hit points for anything that can be hurt: the hero, rats and the Rat King. Other components
    /// react through the events (knockback, flashing, dying) instead of Health knowing about them.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Tooltip("Hit points when full.")]
        [SerializeField] int maxHealth = 3;
        [Tooltip("Seconds after a hit during which further hits are ignored. 0 for enemies.")]
        [SerializeField] float invulnerableTime;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead => Current <= 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>Raised after taking damage, with the amount and the world position the hit came from.</summary>
        public event Action<int, Vector2> Damaged;
        public event Action Died;
        /// <summary>Raised whenever Current or Max changes, e.g. for the hearts UI.</summary>
        public event Action Changed;

        float invulnerableUntil;

        void Awake() => Current = maxHealth;

        /// <summary>Applies damage unless dead or still invulnerable. Returns whether it landed.</summary>
        public bool TakeDamage(int amount, Vector2 source)
        {
            if (amount <= 0 || IsDead || IsInvulnerable)
                return false;

            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerableTime;
            Damaged?.Invoke(amount, source);
            Changed?.Invoke();
            if (IsDead)
                Died?.Invoke();
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead)
                return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke();
        }

        /// <summary>Changes the maximum, e.g. when the Heart item adds one.</summary>
        public void SetMax(int value, bool refill)
        {
            maxHealth = Mathf.Max(1, value);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
            Changed?.Invoke();
        }

        /// <summary>Back to full health with no invulnerability, e.g. when respawning.</summary>
        public void Restore()
        {
            Current = maxHealth;
            invulnerableUntil = 0f;
            Changed?.Invoke();
        }
    }
}
