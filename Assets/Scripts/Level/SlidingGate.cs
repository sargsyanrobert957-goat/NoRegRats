using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// A gate that slides between closed (solid, where it is placed) and open (moved by Open Offset,
    /// not solid). Used for the arena entrance and exit.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SlidingGate : MonoBehaviour
    {
        [Tooltip("From the closed position to the open one, in world units.")]
        [SerializeField] Vector2 openOffset = new Vector2(0f, 10f);
        [SerializeField] bool startOpen;
        [Tooltip("Seconds it takes to open or close.")]
        [SerializeField] float slideTime = 0.3f;

        public bool IsOpen { get; private set; }

        Collider2D gateCollider;
        Vector3 closedPosition;
        Coroutine sliding;

        void Awake()
        {
            gateCollider = GetComponent<Collider2D>();
            closedPosition = transform.position;
            IsOpen = startOpen;
            transform.position = Target(IsOpen);
            gateCollider.enabled = !IsOpen;
        }

        public void Open() => Slide(true);
        public void Close() => Slide(false);

        void Slide(bool open)
        {
            if (open == IsOpen)
                return;
            IsOpen = open;
            Sfx.Play(Sfx.Sound.Gate);
            if (!open)
                gateCollider.enabled = true;
            if (sliding != null)
                StopCoroutine(sliding);
            sliding = StartCoroutine(SlideTo(Target(open)));
        }

        Vector3 Target(bool open) => open ? closedPosition + (Vector3)openOffset : closedPosition;

        IEnumerator SlideTo(Vector3 target)
        {
            Vector3 start = transform.position;
            for (float t = 0f; t < slideTime; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / slideTime));
                yield return null;
            }
            transform.position = target;
            if (IsOpen)
                gateCollider.enabled = false;
            sliding = null;
        }
    }
}
