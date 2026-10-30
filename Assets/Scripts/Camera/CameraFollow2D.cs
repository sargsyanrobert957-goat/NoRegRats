using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Smoothly follows a target, usually the player. Put it on the camera. Looks a little ahead in
    /// the direction the player faces and further down while they fall fast (dropping into the
    /// sewers), never shows anything outside the level bounds, and can be locked onto a fixed spot
    /// (the boss arena).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Offset from the target. A little upward shows more of what is above the player.")]
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
        [Tooltip("Roughly how many seconds the camera takes to catch up. 0 follows exactly.")]
        [SerializeField] float smoothTime = 0.15f;

        [Header("Looking Ahead")]
        [Tooltip("How far ahead the camera looks in the direction the player faces, in world units.")]
        [SerializeField] float lookAhead = 1.5f;
        [Tooltip("Seconds the look-ahead takes to swing over after the player turns.")]
        [SerializeField] float lookAheadTime = 0.5f;
        [Tooltip("How far further down the camera looks while the player falls fast.")]
        [SerializeField] float fallLookDown = 2.5f;
        [Tooltip("Falling faster than this (units per second) counts as falling fast.")]
        [SerializeField] float fallLookSpeed = 8f;

        [Header("Bounds")]
        [Tooltip("The view never shows anything outside this area (world units). Width 0 = no limit.")]
        [SerializeField] Rect bounds;

        Camera cam;
        PlayerMovement movement;
        Vector3 velocity;
        float lead;
        float leadVelocity;
        float fallOffset;
        float fallVelocity;
        bool locked;
        Vector2 lockCenter;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target != null)
                movement = target.GetComponent<PlayerMovement>();
        }

        /// <summary>Holds the camera on a fixed point until Unlock, e.g. while fighting the boss.</summary>
        public void LockTo(Vector2 center)
        {
            locked = true;
            lockCenter = center;
        }

        public void Unlock() => locked = false;

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector2 goal;
            if (locked)
            {
                goal = lockCenter;
            }
            else
            {
                float facing = movement != null ? movement.FacingDirection : 0f;
                lead = Mathf.SmoothDamp(lead, facing * lookAhead, ref leadVelocity, lookAheadTime);
                bool fallingFast = movement != null && movement.Velocity.y < -fallLookSpeed;
                fallOffset = Mathf.SmoothDamp(fallOffset, fallingFast ? -fallLookDown : 0f, ref fallVelocity, 0.25f);
                goal = (Vector2)target.position + offset + new Vector2(lead, fallOffset);
            }

            goal = ClampToBounds(goal);
            var goal3 = new Vector3(goal.x, goal.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal3, ref velocity, smoothTime);
        }

        Vector2 ClampToBounds(Vector2 center)
        {
            if (bounds.width <= 0f || !cam.orthographic)
                return center;
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            center.x = bounds.width < 2f * halfWidth ? bounds.center.x : Mathf.Clamp(center.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            center.y = bounds.height < 2f * halfHeight ? bounds.center.y : Mathf.Clamp(center.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);
            return center;
        }
    }
}
