using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Fades the screen to black, reloads the current level and fades back in. Creates its own
    /// full-screen overlay the first time it's used and keeps it across scene loads.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;

        CanvasGroup group;
        bool busy;

        /// <summary>Fade out, reload the active scene, fade in. Ignored while already fading.</summary>
        public static void ReloadScene(float fadeTime = 0.35f)
        {
            if (instance == null)
                instance = Create();
            if (!instance.busy)
                instance.StartCoroutine(instance.Reload(fadeTime));
        }

        static ScreenFader Create()
        {
            var root = new GameObject("Screen Fader");
            DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var fader = root.AddComponent<ScreenFader>();
            fader.group = root.AddComponent<CanvasGroup>();
            fader.group.alpha = 0f;
            fader.group.blocksRaycasts = false;

            var black = new GameObject("Black").AddComponent<Image>();
            black.transform.SetParent(root.transform, false);
            black.color = Color.black;
            black.rectTransform.anchorMin = Vector2.zero;
            black.rectTransform.anchorMax = Vector2.one;
            black.rectTransform.offsetMin = Vector2.zero;
            black.rectTransform.offsetMax = Vector2.zero;
            return fader;
        }

        IEnumerator Reload(float fadeTime)
        {
            busy = true;
            yield return Fade(0f, 1f, fadeTime);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield return null;
            yield return Fade(1f, 0f, fadeTime);
            busy = false;
        }

        // Unscaled time, so it still works if the game is paused.
        IEnumerator Fade(float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
