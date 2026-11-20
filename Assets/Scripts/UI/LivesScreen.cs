using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The full-screen "hero × 3" shown when a run starts and after each death, with the game frozen
    /// behind it. After a death the old number is struck out and the new one pops in; the last life
    /// is red and shakes. It goes after a couple of seconds, or sooner on any key or click once the
    /// number is up. Not shown over the title, after Continue, or when the last life is lost (You
    /// Lost shows instead).
    /// </summary>
    public class LivesScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [Tooltip("The lives count.")]
        [SerializeField] Image digit;
        [Tooltip("The count before the death, struck out, in the same place.")]
        [SerializeField] Image oldDigit;
        [Tooltip("The stroke drawn across the old count (a Filled image).")]
        [SerializeField] Image strike;
        [Tooltip("Drawn numbers: element 0 is 1, element 1 is 2, and so on.")]
        [SerializeField] Sprite[] numbers;
        [SerializeField] Color ink = new Color(0.1f, 0.1f, 0.1f);
        [Tooltip("Colour of the number on the last life.")]
        [SerializeField] Color lastLifeColor = new Color(0.75f, 0.16f, 0.16f);

        [Header("Timing (seconds)")]
        [Tooltip("How long the screen stays once the number is up.")]
        [SerializeField] float showTime = 2f;
        [Tooltip("How long the old count shows before it is struck out.")]
        [SerializeField] float oldCountTime = 0.5f;
        [SerializeField] float strikeTime = 0.25f;
        [Tooltip("The old count shrinking away and the new one popping in.")]
        [SerializeField] float popTime = 0.3f;
        [Tooltip("How long the last-life number shakes.")]
        [SerializeField] float shakeTime = 0.45f;
        [SerializeField] float fadeTime = 0.25f;

        static LivesScreen instance;
        // The count the player last saw, so a death can show it going down.
        static int shownLives;

        CanvasGroup group;
        bool skipped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch() => shownLives = 0;

        /// <summary>A run starting without a reload (Start on the title at launch).</summary>
        public static void ShowNewRun()
        {
            if (instance != null)
                instance.StartCoroutine(instance.Play(0));
        }

        void Awake()
        {
            instance = this;
            group = panel.GetComponent<CanvasGroup>();
            panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // After a reload: a death (the count went down) or a restart. Waits a frame so the title
        // has decided whether it is up; it never shows over the title.
        IEnumerator Start()
        {
            yield return null;
            if (TitleMenu.IsShowing || GameSession.HasLost)
                yield break;
            int before = shownLives > GameSession.Lives ? shownLives : 0;
            yield return Play(before);
        }

        IEnumerator Play(int before)
        {
            int lives = GameSession.Lives;
            shownLives = lives;
            panel.SetActive(true);
            group.alpha = 1f;
            GamePause.Freeze();
            skipped = false;
            IDisposable listener = null;

            digit.enabled = false;
            strike.enabled = false;
            oldDigit.enabled = false;
            if (before > lives && Number(before) != null)
            {
                oldDigit.enabled = true;
                oldDigit.sprite = Number(before);
                oldDigit.color = ink;
                oldDigit.transform.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(oldCountTime);

                strike.enabled = true;
                strike.color = lastLifeColor;
                strike.fillAmount = 0f;
                strike.transform.localScale = Vector3.one;
                for (float t = 0f; t < strikeTime; t += Time.unscaledDeltaTime)
                {
                    strike.fillAmount = t / strikeTime;
                    yield return null;
                }
                strike.fillAmount = 1f;
                yield return new WaitForSecondsRealtime(0.15f);
            }

            digit.enabled = Number(lives) != null;
            digit.sprite = Number(lives);
            bool last = lives == 1;
            digit.color = last ? lastLifeColor : ink;
            Vector3 home = digit.transform.localPosition;
            // From here a key or click sends the screen away.
            listener = InputSystem.onAnyButtonPress.Call(_ => skipped = true);

            for (float t = 0f; t < popTime; t += Time.unscaledDeltaTime)
            {
                float k = t / popTime;
                // Pops in past full size and settles; the old count shrinks and fades as it does.
                digit.transform.localScale = Vector3.one * Overshoot(k);
                Shrink(oldDigit, k);
                Shrink(strike, k);
                yield return null;
            }
            digit.transform.localScale = Vector3.one;
            oldDigit.enabled = false;
            strike.enabled = false;

            float shown = popTime;
            if (last)
            {
                for (float t = 0f; t < shakeTime && !skipped; t += Time.unscaledDeltaTime)
                {
                    float strength = 1f - t / shakeTime;
                    digit.transform.localPosition = home + new Vector3(Mathf.Sin(t * 70f) * 9f, Mathf.Sin(t * 53f) * 4f, 0f) * strength;
                    digit.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 61f) * 7f * strength);
                    yield return null;
                }
                shown += shakeTime;
                digit.transform.localPosition = home;
                digit.transform.localRotation = Quaternion.identity;
            }

            for (float t = shown; t < showTime && !skipped; t += Time.unscaledDeltaTime)
                yield return null;
            listener.Dispose();

            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / fadeTime;
                yield return null;
            }
            panel.SetActive(false);
            GamePause.Unfreeze();
        }

        Sprite Number(int count) => count >= 1 && count <= numbers.Length ? numbers[count - 1] : null;

        static float Overshoot(float k)
        {
            // 0 -> 1.25 -> 1.
            return k < 0.6f ? Mathf.Lerp(0f, 1.25f, k / 0.6f) : Mathf.Lerp(1.25f, 1f, (k - 0.6f) / 0.4f);
        }

        static void Shrink(Image image, float k)
        {
            if (!image.enabled)
                return;
            image.transform.localScale = Vector3.one * (1f - k);
            Color c = image.color;
            c.a = 1f - k;
            image.color = c;
        }
    }
}
