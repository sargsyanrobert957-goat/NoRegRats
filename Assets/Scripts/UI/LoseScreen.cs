using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The "You lost" screen, shown when the hero dies with no lives left. Restart begins a fresh run;
    /// Quit returns to the title.
    /// </summary>
    public class LoseScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;

        void Awake()
        {
            panel.SetActive(false);
            restartButton.onClick.AddListener(Restart);
            quitButton.onClick.AddListener(TitleMenu.ReturnAfterRun);
        }

        void OnEnable() => GameSession.GameLost += OnLost;
        void OnDisable() => GameSession.GameLost -= OnLost;

        void OnLost()
        {
            panel.SetActive(true);
            GamePause.Freeze();
            // The run is over: the level (or boss) music stops. Restart and the title start their own.
            Music.Stop();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }

        void Restart()
        {
            GamePause.Unfreeze();
            GameSession.StartNewRun();
            ScreenFader.ReloadScene();
        }
    }
}
