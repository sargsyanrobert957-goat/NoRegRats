using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// What survives a death. Dying reloads the level, which puts every rat and the Rat King back;
    /// only these values carry over: the death count and whether the Heart was taken.
    /// </summary>
    public static class GameSession
    {
        public static int Deaths { get; private set; }
        public static bool HasHeart { get; set; }

        public static void RecordDeath() => Deaths++;

        /// <summary>Clears everything for a fresh run (e.g. Play or Restart from a menu).</summary>
        public static void StartNewRun()
        {
            Deaths = 0;
            HasHeart = false;
        }

        // Static values would otherwise carry over between Play sessions in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLaunch() => StartNewRun();
    }
}
