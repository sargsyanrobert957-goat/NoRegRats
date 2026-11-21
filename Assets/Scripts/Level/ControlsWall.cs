using System;
using System.Linq;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The controls painted on the brick wall at the start of the street: each row's keys are the
    /// player's current keys (A / D, Space, S, K, Esc by default), written in the hand-drawn capitals
    /// and repainted when a key is changed. The action words and arrows are the artist's drawings,
    /// placed by StartWallSetup.
    /// </summary>
    public class ControlsWall : MonoBehaviour
    {
        [Serializable]
        public struct Row
        {
            [Tooltip("The controls whose keys this row shows, joined with \" / \" (Run left / Run right).")]
            public KeyBindings.Control[] controls;
            public DrawnText keys;
        }

        [SerializeField] Row[] rows;

        void OnEnable()
        {
            KeyBindings.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => KeyBindings.Changed -= Refresh;

        void Refresh()
        {
            foreach (Row row in rows)
                if (row.keys != null && row.controls != null)
                    row.keys.SetText(string.Join(" / ", row.controls.Select(KeyBindings.KeyName)));
        }
    }
}
