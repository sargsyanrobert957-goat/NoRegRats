using System;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// What survives a death. Dying reloads the level, which puts every rat and the Rat King back;
    /// only these values carry over: the death count and whether the Heart was taken. Also holds
    /// whether the run is won and how long it has taken.
    /// </summary>
    public static class GameSession
    {
        public static int Deaths { get; private set; }
        public static bool HasHeart { get; set; }
        public static bool HasWon { get; private set; }

        /// <summary>Seconds of play since the run started (game time, so pauses don't count).</summary>
        public static float RunTime => (HasWon ? endTime : Time.time) - startTime;

        static float startTime;
        static float endTime;

        /// <summary>Raised when the hero walks out of the arena's exit.</summary>
        public static event Action GameWon;

        public static void RecordDeath() => Deaths++;

        public static void Win()
        {
            endTime = Time.time;
            HasWon = true;
            GameWon?.Invoke();
        }

        /// <summary>Clears everything for a fresh run (e.g. Play or Restart from a menu).</summary>
        public static void StartNewRun()
        {
            Deaths = 0;
            HasHeart = false;
            HasWon = false;
            startTime = Time.time;
        }

        // Static values would otherwise carry over between Play sessions in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch() => StartNewRun();
    }
}
