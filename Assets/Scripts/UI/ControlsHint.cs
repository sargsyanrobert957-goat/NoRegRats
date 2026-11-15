using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRegRats
{
    /// <summary>
    /// Shows the controls sheet in the corner when play first starts. It fades out once the hero has
    /// run and jumped, or after a set time.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ControlsHint : MonoBehaviour
    {
        [Tooltip("Seconds it stays up at least, even if the keys are used straight away.")]
        [SerializeField] float minimumTime = 3f;
        [Tooltip("Seconds after which it fades out anyway.")]
        [SerializeField] float maximumTime = 12f;
        [Tooltip("Seconds the fade takes.")]
        [SerializeField] float fadeTime = 0.8f;

        static bool shownThisLaunch;

        CanvasGroup group;
        InputAction moveAction;
        InputAction jumpAction;
        float shownFor;
        bool ran;
        bool jumped;
        bool fading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch() => shownThisLaunch = false;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            moveAction = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: true);
            jumpAction = InputSystem.actions.FindAction("Player/Jump", throwIfNotFound: true);
        }

        void Start()
        {
            if (shownThisLaunch)
            {
                gameObject.SetActive(false);
                return;
            }
            shownThisLaunch = true;
            group.alpha = 1f;
        }

        void Update()
        {
            // Time only counts during play, so it isn't used up behind the title or the pause menu.
            if (GamePause.IsFrozen)
                return;

            shownFor += Time.deltaTime;
            ran |= Mathf.Abs(moveAction.ReadValue<Vector2>().x) > 0.5f;
            jumped |= jumpAction.WasPressedThisFrame();
            if (!fading && shownFor >= minimumTime && ((ran && jumped) || shownFor >= maximumTime))
                fading = true;
            if (!fading)
                return;

            group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.deltaTime / fadeTime);
            if (group.alpha <= 0f)
                gameObject.SetActive(false);
        }
    }
}
