using UnityEngine;

namespace NoRegRats
{
    /// <summary>Smoothly follows a target, usually the player. Put it on the camera.</summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Offset from the target. A little upward shows more of what is above the player.")]
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
        [Tooltip("Roughly how many seconds the camera takes to catch up. 0 follows exactly.")]
        [SerializeField] float smoothTime = 0.15f;

        Vector3 velocity;

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 goal = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
