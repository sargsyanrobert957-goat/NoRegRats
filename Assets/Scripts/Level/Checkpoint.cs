using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A checkpoint marker (a drawn sign) with a trigger. Walking past it saves the run's progress
    /// there: its tick appears with a puff and a sound. After a death the level reloads and the hero
    /// starts at the last checkpoint reached (with full hearts, a life down), the camera jumps there,
    /// and any enemy within <c>safeRadius</c> is gone, so a respawn never lands next to one. Index 0
    /// is the level's start and is reached from the beginning.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Order along the level: 0 is the start, then 1, 2, 3.")]
        [SerializeField] int index;
        [Tooltip("The tick drawn on the sign, shown once the checkpoint is reached.")]
        [SerializeField] SpriteRenderer tick;
        [Tooltip("Enemies closer than this to the checkpoint are removed when the hero respawns here.")]
        [SerializeField] float safeRadius = 8f;
        [Tooltip("Goal shown when the hero respawns here (empty: leave the goal as it is).")]
        [SerializeField] string respawnGoal;
        [Tooltip("Seconds the tick takes to pop in.")]
        [SerializeField] float popTime = 0.3f;
        [Tooltip("How far right of the sign the hero respawns, so the sign isn't hidden behind them.")]
        [SerializeField] float respawnOffset = 0.9f;

        static readonly List<Checkpoint> all = new List<Checkpoint>();

        public int Index => index;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            bool reached = index <= GameSession.Checkpoint;
            tick.enabled = reached;
            tick.transform.localScale = Vector3.one;
            if (index == GameSession.Checkpoint && index > 0)
                Respawn();
        }

        // Puts the hero here, standing on the ground the sign stands on.
        void Respawn()
        {
            var hero = FindAnyObjectByType<PlayerMovement>();
            if (hero == null)
                return;
            var body = hero.GetComponent<Rigidbody2D>();
            var heroCollider = hero.GetComponent<Collider2D>();
            float feetBelowCentre = hero.transform.position.y - heroCollider.bounds.min.y;
            var spot = new Vector2(transform.position.x + respawnOffset, transform.position.y + feetBelowCentre + 0.02f);
            body.position = spot;
            hero.transform.position = spot;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            foreach (RatEnemy rat in FindObjectsByType<RatEnemy>())
                if (Vector2.Distance(rat.transform.position, transform.position) < safeRadius)
                    rat.gameObject.SetActive(false);
            foreach (RatShooter rat in FindObjectsByType<RatShooter>())
                if (Vector2.Distance(rat.transform.position, transform.position) < safeRadius)
                    rat.gameObject.SetActive(false);

            CameraFollow2D.SnapActive();
            if (!string.IsNullOrEmpty(respawnGoal) && !GameSession.HasHeart)
            {
                var goal = FindAnyObjectByType<GoalText>();
                if (goal != null)
                    goal.Show(respawnGoal);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (index <= GameSession.Checkpoint || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerMovement _))
                return;
            GameSession.Checkpoint = index;
            StartCoroutine(Reach());
        }

        IEnumerator Reach()
        {
            tick.enabled = true;
            Sfx.Play(Sfx.Sound.Checkpoint);
            PuffSpawner.Burst(tick.transform.position, 8, 0.8f, 3f, new Color(0.95f, 0.94f, 0.9f, 0.95f));
            for (float t = 0f; t < popTime; t += Time.deltaTime)
            {
                float k = t / popTime;
                // Pops in past full size and settles.
                float s = k < 0.6f ? Mathf.Lerp(0f, 1.3f, k / 0.6f) : Mathf.Lerp(1.3f, 1f, (k - 0.6f) / 0.4f);
                tick.transform.localScale = Vector3.one * s;
                yield return null;
            }
            tick.transform.localScale = Vector3.one;
        }
    }
}
