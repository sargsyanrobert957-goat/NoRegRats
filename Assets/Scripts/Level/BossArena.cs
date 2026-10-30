using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Runs the boss fight. Its trigger covers the arena floor past the entrance; when the hero walks
    /// in, the gate drops behind them, the camera locks onto the arena, the Rat King starts (with a
    /// line in a speech bubble) and his health bar shows. When he's beaten he says a last line; when
    /// the hero takes the Heart he drops, the exit grate opens and the camera is freed. The goal text
    /// follows along. If the Heart was already taken earlier in the run, the exit is simply open.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArena : MonoBehaviour
    {
        [SerializeField] RatKing king;
        [SerializeField] SlidingGate entranceGate;
        [SerializeField] SlidingGate exitGrate;
        [SerializeField] CameraFollow2D cameraFollow;
        [Tooltip("Where the camera is held during the fight.")]
        [SerializeField] Transform cameraSpot;
        [SerializeField] BossHealthBar healthBar;

        [Header("Goals")]
        [SerializeField] GoalText goalText;
        [SerializeField] string fightGoal = "Defeat the Rat King";
        [SerializeField] string heartGoal = "Take the Heart";
        [SerializeField] string escapeGoal = "Escape through the grate!";
        [Tooltip("Goal after dying with the Heart already taken.")]
        [SerializeField] string returnGoal = "Get back to the arena and escape";

        [Header("Rat King's Lines")]
        [SerializeField] SpeechBubble speechBubble;
        [SerializeField, TextArea] string fightLine = "You're in MY sewer now. Hand over the snacks.";
        [SerializeField, TextArea] string defeatLine = "This... isn't over... I'm calling my cousin the possum...";
        [Tooltip("Seconds each line stays up.")]
        [SerializeField] float lineTime = 3.5f;
        [Tooltip("Height of the bubble's tail above the Rat King's feet.")]
        [SerializeField] float kingSpeechHeight = 3.3f;

        bool started;

        void Start()
        {
            if (GameSession.HasHeart)
            {
                started = true;
                exitGrate.Open();
                goalText.Show(returnGoal);
            }
        }

        void OnEnable()
        {
            king.Defeated += OnKingDefeated;
            HeartPickup.Collected += OnHeartCollected;
        }

        void OnDisable()
        {
            king.Defeated -= OnKingDefeated;
            HeartPickup.Collected -= OnHeartCollected;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (started || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerMovement _))
                return;
            started = true;
            entranceGate.Close();
            cameraFollow.LockTo(cameraSpot.position);
            king.StartFight();
            healthBar.Show(king.Health);
            goalText.Show(fightGoal);
            speechBubble.Say(king.transform, kingSpeechHeight, fightLine, lineTime);
            Music.PlayBoss();
        }

        void OnKingDefeated()
        {
            healthBar.Hide();
            Music.PlayLevel();
            goalText.Show(heartGoal);
            speechBubble.Say(king.transform, kingSpeechHeight, defeatLine, lineTime);
        }

        void OnHeartCollected()
        {
            exitGrate.Open();
            cameraFollow.Unlock();
            goalText.Show(escapeGoal);
        }
    }
}
