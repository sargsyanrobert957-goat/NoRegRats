using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The graph-paper backdrop: a tiled sprite that drifts slower than the level as the camera
    /// moves (parallax) and takes its tint from how deep the camera is, so the subway and the
    /// sewers feel different. Make the sprite big enough to cover the view across the whole level.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [Tooltip("0 = scrolls with the level, 1 = fixed to the camera.")]
        [SerializeField, Range(0f, 1f)] float parallax = 0.6f;
        [Tooltip("Tint by depth: left end = at or below Low Y (sewers), right end = at or above High Y (subway).")]
        [SerializeField] Gradient tint = new Gradient();
        [SerializeField] float lowY = -20f;
        [SerializeField] float highY = -2f;

        SpriteRenderer spriteRenderer;
        Vector3 startPosition;
        Vector3 cameraStart;

        void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        void Start()
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;
            startPosition = transform.position;
            cameraStart = cameraTransform.position;
        }

        void LateUpdate()
        {
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 moved = cameraPosition - cameraStart;
            transform.position = startPosition + new Vector3(moved.x * parallax, moved.y * parallax, 0f);
            spriteRenderer.color = tint.Evaluate(Mathf.InverseLerp(lowY, highY, cameraPosition.y));
        }
    }
}
