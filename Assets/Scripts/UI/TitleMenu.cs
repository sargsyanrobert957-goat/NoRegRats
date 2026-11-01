using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The title screen over the level when the game starts: the game is frozen behind it until
    /// Play, which starts a fresh run. Shown once per launch only, so dying (which reloads the level)
    /// or restarting from a menu goes straight back into the game.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;

        static bool shownThisLaunch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch() => shownThisLaunch = false;

        // The HUD canvas (this object) has no GraphicRaycaster in the scene, and without one the
        // mouse can't reach any of the menus' buttons.
        void Awake()
        {
            if (!TryGetComponent(out GraphicRaycaster _))
                gameObject.AddComponent<GraphicRaycaster>();
        }

        void Start()
        {
            playButton.onClick.AddListener(Play);
            quitButton.onClick.AddListener(GamePause.QuitGame);
            if (shownThisLaunch)
            {
                panel.SetActive(false);
                return;
            }
            shownThisLaunch = true;
            panel.SetActive(true);
            GamePause.Freeze();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        void Play()
        {
            panel.SetActive(false);
            GameSession.StartNewRun();
            GamePause.Unfreeze();
        }
    }
}
