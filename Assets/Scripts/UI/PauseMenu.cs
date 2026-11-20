using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Pauses the game (the pause key, Esc by default; gamepad Start; or the HUD button) and shows
    /// Continue, Restart and Quit next to the controls, where keys can be changed. It doesn't open
    /// over other menus.
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
            // While a key is being changed, Esc cancels that instead.
            if (!pauseAction.WasPressedThisFrame() || KeyBindings.BlocksMenuKeys)
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
            Music.SetPaused(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }

        void Resume()
        {
            paused = false;
            panel.SetActive(false);
            GamePause.Unfreeze();
            Music.SetPaused(false);
            // Nothing stays selected during play, or Space (jump, and the UI's submit) would
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
            // The title plays its own menu music.
            Music.SetPaused(false);
            TitleMenu.ShowOverRun();
        }
    }
}
