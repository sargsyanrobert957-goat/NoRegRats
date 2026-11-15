using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The "You Won" screen with the run's stats (time, deaths, hearts left). Play Again begins a fresh
    /// run; Quit returns to the title.
    /// </summary>
    public class WinScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text timeValue;
        [SerializeField] Text deathsValue;
        [SerializeField] Text heartsValue;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button quitButton;
        [SerializeField] Health playerHealth;
        [Tooltip("Seconds between walking out and the screen appearing.")]
        [SerializeField] float delay = 0.6f;

        void Awake()
        {
            panel.SetActive(false);
            playAgainButton.onClick.AddListener(PlayAgain);
            quitButton.onClick.AddListener(TitleMenu.ReturnAfterRun);
        }

        void OnEnable() => GameSession.GameWon += OnWon;
        void OnDisable() => GameSession.GameWon -= OnWon;

        void OnWon() => StartCoroutine(Show());

        IEnumerator Show()
        {
            yield return new WaitForSecondsRealtime(delay);
            int seconds = Mathf.FloorToInt(GameSession.RunTime);
            timeValue.text = $"{seconds / 60}:{seconds % 60:00}";
            deathsValue.text = GameSession.Deaths.ToString();
            heartsValue.text = $"{playerHealth.Current} / {playerHealth.Max}";
            panel.SetActive(true);
            GamePause.Freeze();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(playAgainButton.gameObject);
        }

        void PlayAgain()
        {
            GamePause.Unfreeze();
            GameSession.StartNewRun();
            ScreenFader.ReloadScene();
        }
    }
}
