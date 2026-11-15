using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Pause (Esc / gamepad Start, the "UI/Pause" action, or the pause button on the HUD): freezes
    /// the game and shows Continue, Restart (a fresh run) and Quit (back to the title, where
    /// Continue returns to this run), next to the controls. Pressing Pause again continues.
    /// Doesn't open over the other menus.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button continueButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;
        [Tooltip("The button on the HUD that opens this during play.")]
        [SerializeField] Button pauseButton;

        InputAction pauseAction;
        bool paused;

        void Awake()
        {
            pauseAction = InputSystem.actions.FindAction("UI/Pause", throwIfNotFound: true);
            panel.SetActive(false);
            continueButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(Restart);
            quitButton.onClick.AddListener(QuitToTitle);
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() =>
                {
                    if (!paused && !GamePause.IsFrozen)
                        Pause();
                });
        }

        void Update()
        {
            if (!pauseAction.WasPressedThisFrame())
                return;
            if (paused)
                Resume();
            else if (!GamePause.IsFrozen)
                Pause();
        }

        void Pause()
        {
            paused = true;
            panel.SetActive(true);
            GamePause.Freeze();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }

        void Resume()
        {
            paused = false;
            panel.SetActive(false);
            GamePause.Unfreeze();
            // Nothing stays selected during play, or Space (attack, and the UI's submit) would
            // press the HUD's pause button again.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        void Restart()
        {
            Resume();
            GameSession.StartNewRun();
            ScreenFader.ReloadScene();
        }

        // The game stays frozen under the title.
        void QuitToTitle()
        {
            paused = false;
            panel.SetActive(false);
            TitleMenu.ShowOverRun();
        }
    }
}
