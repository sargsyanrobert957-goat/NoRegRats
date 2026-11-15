using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The Heart the Rat King drops. It bobs in place; taking it adds one max heart (refilled) and is
    /// remembered after dying.
    /// </summary>
    public class HeartPickup : MonoBehaviour
    {
        [SerializeField] float bobHeight = 0.15f;
        [Tooltip("Bobs per second, times 2π.")]
        [SerializeField] float bobSpeed = 3f;

        Vector3 startPosition;

        void Start() => startPosition = transform.position;

        void Update() => transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerHealth playerHealth))
                return;
            playerHealth.GainHeart();
            GameSession.HasHeart = true;
            Sfx.Play(Sfx.Sound.Heart);
            PuffSpawner.Burst(transform.position, 10, 1f, 3f, new Color(1f, 0.82f, 0.35f, 0.95f));
            Destroy(gameObject);
        }
    }
}
