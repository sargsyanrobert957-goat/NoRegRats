using UnityEngine;

namespace NoRegRats
{
    /// <summary>Changes the goal text when the hero walks into its trigger, e.g. on reaching the sewers.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalTrigger : MonoBehaviour
    {
        [SerializeField] GoalText goalText;
        [SerializeField] string goal = "Find the Rat King";

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out PlayerMovement _))
                goalText.Show(goal);
        }
    }
}
