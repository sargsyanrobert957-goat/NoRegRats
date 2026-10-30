using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Pause (Esc / gamepad Start, the "UI/Pause" action): freezes the game and shows the controls
    /// with Resume, Restart (a fresh run) and Quit. Pressing Pause again resumes. Doesn't open over
    /// the title or win screens.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;

        InputAction pauseAction;
        bool paused;

        void Awake()
        {
            pauseAction = InputSystem.actions.FindAction("UI/Pause", throwIfNotFound: true);
            panel.SetActive(false);
            resumeButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(Restart);
            quitButton.onClick.AddListener(GamePause.QuitGame);
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
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        void Resume()
        {
            paused = false;
            panel.SetActive(false);
            GamePause.Unfreeze();
        }

        void Restart()
        {
            Resume();
            GameSession.StartNewRun();
            ScreenFader.ReloadScene();
        }
    }
}
