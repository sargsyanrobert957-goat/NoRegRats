using System;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Sound effects. Gameplay calls Sfx.Play(Sound.X); each sound has one or more clips (one chosen at
    /// random, with slight pitch variation) and a volume. A sound with no clips is silent.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Sfx : MonoBehaviour
    {
        // Saved by number in the scenes, so add new sounds at the end. Punch was Shoot.
        // Gunshot is for the shooting rat (Rat2); Push is the scrape while a box or bin is pushed.
        public enum Sound { Jump, Land, Step, Punch, Hit, RatPoof, Hurt, KingThrow, KingDefeated, Heart, Gate, Click, Gunshot, Push }

        [Serializable]
        struct Entry
        {
            public Sound sound;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] Entry[] sounds =
        {
            new Entry { sound = Sound.Jump, volume = 0.7f },
            new Entry { sound = Sound.Land, volume = 0.6f },
            new Entry { sound = Sound.Step, volume = 0.35f },
            new Entry { sound = Sound.Punch, volume = 0.7f },
            new Entry { sound = Sound.Hit, volume = 0.8f },
            new Entry { sound = Sound.RatPoof, volume = 0.8f },
            new Entry { sound = Sound.Hurt, volume = 0.9f },
            new Entry { sound = Sound.KingThrow, volume = 0.8f },
            new Entry { sound = Sound.KingDefeated, volume = 1f },
            new Entry { sound = Sound.Heart, volume = 1f },
            new Entry { sound = Sound.Gate, volume = 0.8f },
            new Entry { sound = Sound.Click, volume = 0.6f },
            new Entry { sound = Sound.Gunshot, volume = 0.8f },
            new Entry { sound = Sound.Push, volume = 0.6f },
        };
        [Tooltip("Each play is pitched randomly within this range.")]
        [SerializeField] Vector2 pitchRange = new Vector2(0.94f, 1.06f);

        static Sfx instance;
        AudioSource source;

        void Awake()
        {
            instance = this;
            source = GetComponent<AudioSource>();
            // Menu clicks still sound while the pause menu holds the music.
            source.ignoreListenerPause = true;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void Play(Sound sound)
        {
            if (instance != null)
                instance.PlaySound(sound);
        }

        void PlaySound(Sound sound)
        {
            foreach (Entry entry in sounds)
            {
                if (entry.sound != sound || entry.clips == null || entry.clips.Length == 0)
                    continue;
                AudioClip clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
                if (clip == null)
                    return;
                source.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
                source.PlayOneShot(clip, entry.volume);
                return;
            }
        }
    }
}
