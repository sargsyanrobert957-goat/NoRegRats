using System;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// What survives a death. Dying reloads the level, which puts every rat and the Rat King back;
    /// only these values carry over: the lives left, the death count and whether the Heart was
    /// taken. Also holds whether the run is won or lost and how long it has taken.
    /// </summary>
    public static class GameSession
    {
        /// <summary>Lives at the start of a run. Losing the last one ends the run.</summary>
        public const int StartingLives = 3;

        public static int Lives { get; private set; }
        public static int Deaths { get; private set; }
        public static bool HasHeart { get; set; }
        public static bool HasWon { get; private set; }
        public static bool HasLost { get; private set; }

        /// <summary>Seconds of play since the run started (game time, so pauses don't count).</summary>
        public static float RunTime => (HasWon ? endTime : Time.time) - startTime;

        static float startTime;
        static float endTime;

        /// <summary>Raised when the hero walks out of the arena's exit.</summary>
        public static event Action GameWon;
        /// <summary>Raised when the hero dies with no lives left.</summary>
        public static event Action GameLost;

        /// <summary>Counts a death and takes a life.</summary>
        public static void RecordDeath()
        {
            Deaths++;
            Lives = Mathf.Max(0, Lives - 1);
        }

        public static void Win()
        {
            endTime = Time.time;
            HasWon = true;
            GameWon?.Invoke();
        }

        public static void Lose()
        {
            HasLost = true;
            GameLost?.Invoke();
        }

        /// <summary>Clears everything for a fresh run (e.g. Start or Restart from a menu).</summary>
        public static void StartNewRun()
        {
            Lives = StartingLives;
            Deaths = 0;
            HasHeart = false;
            HasWon = false;
            HasLost = false;
            startTime = Time.time;
        }

        // Static values would otherwise carry over between Play sessions in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch() => StartNewRun();
    }
}
