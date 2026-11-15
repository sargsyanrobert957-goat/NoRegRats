using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The boss health bar: hidden until the fight starts, then its fill shrinks with his health.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Tooltip("Shown during the fight, hidden otherwise.")]
        [SerializeField] GameObject panel;
        [SerializeField] RectTransform fill;

        Health boss;

        void Awake() => panel.SetActive(false);

        public void Show(Health health)
        {
            Hide();
            boss = health;
            boss.Changed += Refresh;
            panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (boss != null)
                boss.Changed -= Refresh;
            boss = null;
            panel.SetActive(false);
        }

        void OnDestroy() => Hide();

        void Refresh() => fill.anchorMax = new Vector2((float)boss.Current / boss.Max, 1f);
    }
}
