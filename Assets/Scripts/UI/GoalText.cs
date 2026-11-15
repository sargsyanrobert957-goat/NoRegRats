using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The current goal at the top of the screen. GoalTrigger and BossArena change it as the hero
    /// progresses (none is shown at the start); each change pops the text bigger for a moment so it gets noticed.
    /// </summary>
    public class GoalText : MonoBehaviour
    {
        [SerializeField] Text label;
        [Tooltip("Written before every goal.")]
        [SerializeField] string prefix = "Goal: ";
        [Tooltip("How much bigger the text pops when the goal changes.")]
        [SerializeField] float popScale = 1.3f;
        [Tooltip("Seconds the pop takes to settle.")]
        [SerializeField] float popTime = 0.4f;

        string goal;
        float popTimer;

        // Nothing is shown when the level starts; the first goal appears when the hero reaches it.
        void Awake() => label.text = "";

        public void Show(string newGoal)
        {
            if (newGoal == goal)
                return;
            goal = newGoal;
            label.text = prefix + newGoal;
            popTimer = popTime;
        }

        void Update()
        {
            popTimer = Mathf.Max(0f, popTimer - Time.unscaledDeltaTime);
            float scale = Mathf.Lerp(1f, popScale, popTimer / popTime);
            label.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
