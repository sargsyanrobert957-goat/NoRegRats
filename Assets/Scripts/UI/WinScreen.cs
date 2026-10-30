using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// "You escaped!" with the run's stats (time, deaths, hearts left), shown a moment after the
    /// hero walks out of the exit. Play Again starts a fresh run; Quit leaves the game.
    /// </summary>
    public class WinScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text stats;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button quitButton;
        [SerializeField] Health playerHealth;
        [Tooltip("Seconds between walking out and the screen appearing.")]
        [SerializeField] float delay = 0.6f;

        void Awake()
        {
            panel.SetActive(false);
            playAgainButton.onClick.AddListener(PlayAgain);
            quitButton.onClick.AddListener(GamePause.QuitGame);
        }

        void OnEnable() => GameSession.GameWon += OnWon;
        void OnDisable() => GameSession.GameWon -= OnWon;

        void OnWon() => StartCoroutine(Show());

        IEnumerator Show()
        {
            yield return new WaitForSecondsRealtime(delay);
            int seconds = Mathf.FloorToInt(GameSession.RunTime);
            stats.text = $"Time   {seconds / 60}:{seconds % 60:00}\n" +
                         $"Deaths   {GameSession.Deaths}\n" +
                         $"Hearts left   {playerHealth.Current} / {playerHealth.Max}";
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
