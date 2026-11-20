using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRegRats
{
    /// <summary>
    /// The player's own keyboard keys: one key per control, changed on the controls screen, saved
    /// between launches (PlayerPrefs) and put back to the defaults by Reset. Choosing a key another
    /// control already uses swaps the two. Mouse and gamepad bindings are left as they are.
    /// </summary>
    public static class KeyBindings
    {
        public enum Control { RunLeft, RunRight, Jump, Crouch, Punch, Pause }

        const string SaveKey = "NoRegRats.KeyBindings";
        // After a key press finishes a change, menus ignore that press (it could also be Submit or Pause).
        const float SettleTime = 0.25f;

        static readonly Control[] All = (Control[])Enum.GetValues(typeof(Control));

        /// <summary>Raised whenever a key changes (a new key, a swap or Reset).</summary>
        public static event Action Changed;

        public static bool IsRebinding => operation != null;

        /// <summary>True while waiting for a key, and just after: menus shouldn't react to that press.</summary>
        public static bool BlocksMenuKeys => IsRebinding || Time.unscaledTime < settleUntil;

        public static Control? Rebinding { get; private set; }

        static InputActionRebindingExtensions.RebindingOperation operation;
        static InputAction rebindingAction;
        static bool reenableAfter;
        static float settleUntil;
        static bool cancelledFromCode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            operation = null;
            Rebinding = null;
            settleUntil = 0f;
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
                return;
            // In the editor the asset keeps the last play's keys, so start from the defaults.
            actions.RemoveAllBindingOverrides();
            string saved = PlayerPrefs.GetString(SaveKey, "");
            if (saved.Length > 0)
            {
                try
                {
                    actions.LoadBindingOverridesFromJson(saved);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("KeyBindings: couldn't read the saved keys, using the defaults. " + e.Message);
                    actions.RemoveAllBindingOverrides();
                }
            }
        }

        /// <summary>The key's name as shown on the key box ("A", "Space", "L Shift").</summary>
        public static string KeyName(Control control)
        {
            (InputAction action, int index) = Find(control);
            if (action == null)
                return "-";
            return Shorten(action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions));
        }

        /// <summary>Waits for the next key press and gives it to the control; Esc cancels (click away for Pause).</summary>
        public static void Rebind(Control control)
        {
            CancelRebind();
            (InputAction action, int index) = Find(control);
            if (action == null)
                return;

            string before = action.bindings[index].effectivePath;
            // Bindings can only change while their action is off.
            reenableAfter = action.enabled;
            if (reenableAfter)
                action.Disable();
            rebindingAction = action;
            Rebinding = control;

            var rebind = action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithMatchingEventsBeingSuppressed()
                .OnComplete(_ => Finish(control, before, true))
                .OnCancel(_ => Finish(control, before, false));
            // Esc is the cancel key, except when choosing the pause key, which may be Esc itself.
            if (control != Control.Pause)
                rebind.WithCancelingThrough("<Keyboard>/escape");
            operation = rebind;
            operation.Start();
            Changed?.Invoke();
        }

        /// <summary>Stops waiting for a key, keeping the old one.</summary>
        public static void CancelRebind()
        {
            if (operation == null)
                return;
            cancelledFromCode = true;
            operation.Cancel();
            cancelledFromCode = false;
        }

        /// <summary>Back to A / D, Space, S, K and Esc.</summary>
        public static void ResetToDefaults()
        {
            CancelRebind();
            foreach (Control control in All)
            {
                (InputAction action, int index) = Find(control);
                action?.RemoveBindingOverride(index);
            }
            Save();
            Changed?.Invoke();
        }

        static void Finish(Control control, string before, bool chosen)
        {
            operation.Dispose();
            operation = null;
            Rebinding = null;
            if (reenableAfter)
                rebindingAction.Enable();
            rebindingAction = null;
            // Only a key press needs ignoring afterwards, not a click or a menu closing.
            if (!cancelledFromCode)
                settleUntil = Time.unscaledTime + SettleTime;

            if (chosen)
            {
                // The key's old owner gets this control's old key.
                string now = Path(control);
                foreach (Control other in All)
                {
                    if (other == control || !string.Equals(Path(other), now, StringComparison.OrdinalIgnoreCase))
                        continue;
                    (InputAction action, int index) = Find(other);
                    action.ApplyBindingOverride(index, before);
                }
                Save();
            }
            Changed?.Invoke();
        }

        static string Path(Control control)
        {
            (InputAction action, int index) = Find(control);
            return action == null ? "" : action.bindings[index].effectivePath;
        }

        static void Save()
        {
            PlayerPrefs.SetString(SaveKey, InputSystem.actions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        // The control's keyboard binding: its action, and the binding's index in it.
        static (InputAction, int) Find(Control control)
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
                return (null, -1);
            string actionName = control switch
            {
                Control.RunLeft or Control.RunRight => "Player/Move",
                Control.Jump => "Player/Jump",
                Control.Crouch => "Player/Crouch",
                Control.Punch => "Player/Attack",
                _ => "UI/Pause",
            };
            string part = control == Control.RunLeft ? "left" : control == Control.RunRight ? "right" : null;
            InputAction action = actions.FindAction(actionName, throwIfNotFound: true);
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite != (part != null))
                    continue;
                if (part != null && binding.name != part)
                    continue;
                if (binding.path.StartsWith("<Keyboard>", StringComparison.Ordinal))
                    return (action, i);
            }
            Debug.LogError($"KeyBindings: {actionName} has no keyboard binding.");
            return (null, -1);
        }

        static string Shorten(string name) => name switch
        {
            "Escape" => "Esc",
            "Left Control" or "Control" => "L Ctrl",
            "Right Control" => "R Ctrl",
            "Left Shift" or "Shift" => "L Shift",
            "Right Shift" => "R Shift",
            "Left Alt" or "Alt" => "L Alt",
            "Right Alt" => "R Alt",
            "Up Arrow" => "Up",
            "Down Arrow" => "Down",
            "Left Arrow" => "Left",
            "Right Arrow" => "Right",
            "Left System" or "Left Windows" or "Left Command" => "L Win",
            "Right System" or "Right Windows" or "Right Command" => "R Win",
            "Context Menu" => "Menu",
            _ => name,
        };
    }
}
