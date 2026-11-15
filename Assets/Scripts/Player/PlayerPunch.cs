using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRegRats
{
    /// <summary>
    /// The hero's bare-handed punch: Attack (Space) hits everything with a Health in a short box in
    /// front of the hero, with a cooldown. Every punch lunges the figure forward and puffs at the
    /// fist; a hit also plays the Hit sound and gives a tiny camera shake, a miss plays the Punch
    /// whoosh. Hits use an overlap check, so rats (which pass through the hero) are hit too.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerPunch : MonoBehaviour
    {
        [Tooltip("Hit points taken per punch.")]
        [SerializeField] int damage = 1;
        [Tooltip("Seconds between punches.")]
        [SerializeField] float cooldown = 0.3f;
        [Tooltip("How far the punch reaches past the front of the hero's collider, in world units.")]
        [SerializeField] float reach = 0.9f;
        [Tooltip("Height of the hit box. Centred on the fist, low enough to hit rats (about 1 unit tall).")]
        [SerializeField] float height = 1.1f;
        [Tooltip("Height of the fist above the bottom of the hero's collider.")]
        [SerializeField] float fistHeight = 0.75f;
        [Tooltip("Camera shake when a punch lands, in world units.")]
        [SerializeField] float hitShake = 0.06f;
        [Tooltip("Colour of the puff at the fist.")]
        [SerializeField] Color puffColor = new Color(0.95f, 0.94f, 0.9f, 0.95f);
        [Tooltip("Gets the forward lunge on every punch.")]
        [SerializeField] HeroAnimator heroAnimator;

        PlayerMovement movement;
        Collider2D bodyCollider;
        Health ownHealth;
        InputAction attackAction;
        float nextPunchTime;
        readonly List<Collider2D> overlaps = new List<Collider2D>();
        readonly HashSet<Health> struck = new HashSet<Health>();
        ContactFilter2D filter;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            bodyCollider = GetComponent<Collider2D>();
            ownHealth = GetComponent<Health>();
            attackAction = InputSystem.actions.FindAction("Player/Attack", throwIfNotFound: true);
            if (heroAnimator == null)
                heroAnimator = GetComponentInChildren<HeroAnimator>();
            filter = new ContactFilter2D { useTriggers = false };
        }

        void Update()
        {
            if (attackAction.WasPressedThisFrame() && Time.time >= nextPunchTime && !movement.InputLocked)
                Punch();
        }

        void Punch()
        {
            nextPunchTime = Time.time + cooldown;
            int direction = movement.FacingDirection;
            Bounds bounds = bodyCollider.bounds;
            float front = direction > 0 ? bounds.max.x : bounds.min.x;
            var fist = new Vector2(front + direction * reach * 0.5f, bounds.min.y + fistHeight);

            struck.Clear();
            Physics2D.OverlapBox(fist, new Vector2(reach, height), 0f, filter, overlaps);
            foreach (Collider2D other in overlaps)
            {
                GameObject hit = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
                if (hit.TryGetComponent(out Health health) && health != ownHealth && struck.Add(health))
                    health.TakeDamage(damage, bounds.center);
            }

            var tip = new Vector2(front + direction * reach, fist.y);
            if (struck.Count > 0)
            {
                Sfx.Play(Sfx.Sound.Hit);
                CameraFollow2D.ShakeActive(hitShake, 0.1f);
                PuffSpawner.Burst(tip, 4, 0.6f, 2.5f, puffColor);
            }
            else
            {
                Sfx.Play(Sfx.Sound.Punch);
                PuffSpawner.Spawn(tip, 0.35f, new Vector2(direction * 1.5f, 0f), puffColor);
            }
            if (heroAnimator != null)
                heroAnimator.Punch();
        }

        void OnDrawGizmosSelected()
        {
            var body = GetComponent<Collider2D>();
            if (body == null)
                return;
            Bounds bounds = body.bounds;
            int direction = Application.isPlaying && movement != null ? movement.FacingDirection : 1;
            float front = direction > 0 ? bounds.max.x : bounds.min.x;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(front + direction * reach * 0.5f, bounds.min.y + fistHeight), new Vector3(reach, height, 0f));
        }
    }
}
