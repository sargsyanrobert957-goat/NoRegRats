using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The title screen: Start, Continue and Exit. It shows on launch and after Quit from the pause,
    /// win or lose screens, with the game frozen behind it. Continue is greyed out when there is no run
    /// to return to.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button startButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button exitButton;

        static bool shownThisLaunch;
        static bool showAfterReload;
        static TitleMenu instance;

        bool runInProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch()
        {
            shownThisLaunch = false;
            showAfterReload = false;
        }

        /// <summary>Shows the title over the frozen game, keeping the run so Continue can return to it.</summary>
        public static void ShowOverRun()
        {
            if (instance != null)
                instance.Show(canContinue: true);
        }

        /// <summary>Reloads the level and shows the title over it (the run is over).</summary>
        public static void ReturnAfterRun()
        {
            showAfterReload = true;
            // Cleared before the reload so the level behind the title starts fresh (the Rat King
            // back, no Heart bonus).
            GameSession.StartNewRun();
            GamePause.Unfreeze();
            ScreenFader.ReloadScene();
        }

        // The HUD canvas (this object) has no GraphicRaycaster in the scene, and without one the
        // mouse can't reach any of the menus' buttons.
        void Awake()
        {
            instance = this;
            if (!TryGetComponent(out GraphicRaycaster _))
                gameObject.AddComponent<GraphicRaycaster>();
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void Start()
        {
            startButton.onClick.AddListener(StartRun);
            continueButton.onClick.AddListener(Continue);
            exitButton.onClick.AddListener(GamePause.QuitGame);
            if (shownThisLaunch && !showAfterReload)
            {
                panel.SetActive(false);
                return;
            }
            shownThisLaunch = true;
            showAfterReload = false;
            Show(canContinue: false);
        }

        void Show(bool canContinue)
        {
            runInProgress = canContinue;
            continueButton.interactable = canContinue;
            panel.SetActive(true);
            Music.PlayMenu();
            GamePause.Freeze();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(canContinue ? continueButton.gameObject : startButton.gameObject);
        }

        void StartRun()
        {
            panel.SetActive(false);
            GamePause.Unfreeze();
            GameSession.StartNewRun();
            // Over a run in progress the level has to be put back to its start (the reload
            // restarts the music too).
            if (runInProgress)
                ScreenFader.ReloadScene();
            else
                Music.ResumeGame();
        }

        void Continue()
        {
            panel.SetActive(false);
            GamePause.Unfreeze();
            Music.ResumeGame();
        }
    }
}
