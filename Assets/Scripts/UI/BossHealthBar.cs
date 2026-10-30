using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The boss's health bar at the top of the screen: hidden until the fight starts, then the fill
    /// shrinks with every hit. The fill is a RectTransform whose right anchor tracks the health.
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
