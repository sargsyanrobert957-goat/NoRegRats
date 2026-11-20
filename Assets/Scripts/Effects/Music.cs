using System.Collections;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Background music: menu, level and boss tracks with a short fade between. Silent until tracks are
    /// assigned.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Music : MonoBehaviour
    {
        [SerializeField] AudioClip levelTrack;
        [SerializeField] AudioClip bossTrack;
        [Tooltip("Plays while the title screen is up.")]
        [SerializeField] AudioClip menuTrack;
        [SerializeField, Range(0f, 1f)] float volume = 0.5f;
        [Tooltip("Seconds to fade out the old track and in the new one.")]
        [SerializeField] float fadeTime = 0.6f;
        [Tooltip("Seconds the music takes to cut out when it's stopped (the Rat King beaten).")]
        [SerializeField] float stopTime = 0.15f;

        static Music instance;
        AudioSource source;
        // What plays during the game itself (level or boss), so closing the title screen can go back to it.
        AudioClip gameTrack;
        bool menuShowing;
        // Stopped by Stop(): silent until a track is asked for again or the level reloads.
        bool silenced;

        void Awake()
        {
            instance = this;
            source = GetComponent<AudioSource>();
            source.loop = true;
            AudioListener.pause = false;
        }

        // A pause left on when Play mode stopped would otherwise keep the next play silent.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch() => AudioListener.pause = false;

        /// <summary>
        /// The pause menu is open: the music holds where it is and carries on from there when
        /// unpaused. Sound effects (menu clicks) keep playing; Sfx ignores this pause.
        /// </summary>
        public static void SetPaused(bool paused) => AudioListener.pause = paused;

        void Start()
        {
            gameTrack = levelTrack;
            // The title screen may have asked for the menu track before this ran.
            if (!menuShowing)
                SwitchTo(levelTrack);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void PlayBoss()
        {
            if (instance == null)
                return;
            instance.silenced = false;
            instance.gameTrack = instance.bossTrack;
            if (!instance.menuShowing)
                instance.SwitchTo(instance.bossTrack);
        }

        public static void PlayLevel()
        {
            if (instance == null)
                return;
            instance.silenced = false;
            instance.gameTrack = instance.levelTrack;
            if (!instance.menuShowing)
                instance.SwitchTo(instance.levelTrack);
        }

        /// <summary>The title screen is up: play the menu track (if one is assigned).</summary>
        public static void PlayMenu()
        {
            if (instance == null)
                return;
            instance.menuShowing = true;
            instance.SwitchTo(instance.menuTrack);
        }

        /// <summary>The title screen is closed: back to the level or boss track.</summary>
        public static void ResumeGame()
        {
            if (instance == null)
                return;
            instance.menuShowing = false;
            if (instance.silenced)
                instance.CutOut();
            else
                instance.SwitchTo(instance.gameTrack != null ? instance.gameTrack : instance.levelTrack);
        }

        /// <summary>Cuts the game music (the Rat King is beaten); it stays silent until the level reloads.</summary>
        public static void Stop()
        {
            if (instance == null)
                return;
            instance.silenced = true;
            if (!instance.menuShowing)
                instance.CutOut();
        }

        void CutOut()
        {
            StopAllCoroutines();
            StartCoroutine(FadeToSilence());
        }

        IEnumerator FadeToSilence()
        {
            float from = source.volume;
            for (float t = 0f; source.isPlaying && t < stopTime; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(from, 0f, t / stopTime);
                yield return null;
            }
            source.Stop();
            source.clip = null;
            source.volume = volume;
        }

        void SwitchTo(AudioClip track)
        {
            if (track == null || source.clip == track)
                return;
            StopAllCoroutines();
            StartCoroutine(Crossfade(track));
        }

        IEnumerator Crossfade(AudioClip track)
        {
            for (float t = 0f; source.isPlaying && t < fadeTime; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(volume, 0f, t / fadeTime);
                yield return null;
            }
            source.clip = track;
            source.Play();
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, volume, t / fadeTime);
                yield return null;
            }
            source.volume = volume;
        }
    }
}
