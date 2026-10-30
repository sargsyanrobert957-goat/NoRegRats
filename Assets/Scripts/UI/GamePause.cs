using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRegRats
{
    /// <summary>
    /// Freezes the game behind a menu (title, pause, win): stops time and switches off the hero's
    /// controls (the Player action map) so key presses meant for the menu don't move or shoot.
    /// The UI action map stays on, so menus and the Pause key keep working.
    /// </summary>
    public static class GamePause
    {
        public static bool IsFrozen { get; private set; }

        static InputActionMap PlayerControls => InputSystem.actions != null ? InputSystem.actions.FindActionMap("Player") : null;

        public static void Freeze()
        {
            IsFrozen = true;
            Time.timeScale = 0f;
            PlayerControls?.Disable();
        }

        public static void Unfreeze()
        {
            IsFrozen = false;
            Time.timeScale = 1f;
            PlayerControls?.Enable();
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // A menu left open when Play mode stopped would otherwise leave time and controls off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetOnLaunch() => Unfreeze();
    }
}
