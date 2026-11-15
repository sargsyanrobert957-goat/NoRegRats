using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// "You lost the game =(" shown when the hero dies with no lives left. Restart starts a fresh
    /// run (all progress is lost); Quit goes back to the title.
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
