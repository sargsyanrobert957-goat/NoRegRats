using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// The hero's hearts in the corner of the screen: one icon per max heart, full or empty, kept in
    /// sync with the hero's Health. When the max goes up (the Heart item) a "+1" pops up beside them.
    /// Icons are copies of a hidden template Image under a layout group.
    /// </summary>
    public class HeartsDisplay : MonoBehaviour
    {
        [SerializeField] Health playerHealth;
        [Tooltip("Hidden Image copied once per heart.")]
        [SerializeField] Image heartTemplate;
        [SerializeField] Sprite fullHeart;
        [SerializeField] Sprite emptyHeart;
        [Tooltip("Shown for a moment when max hearts goes up.")]
        [SerializeField] GameObject bonusPopup;
        [Tooltip("Seconds the bonus popup stays up.")]
        [SerializeField] float popupTime = 1.8f;

        readonly List<Image> hearts = new List<Image>();
        int shownMax;
        float popupUntil;

        void Awake()
        {
            heartTemplate.gameObject.SetActive(false);
            bonusPopup.SetActive(false);
        }

        void OnEnable() => playerHealth.Changed += Refresh;
        void OnDisable() => playerHealth.Changed -= Refresh;
        void Start() => Refresh();

        void Refresh()
        {
            while (hearts.Count < playerHealth.Max)
                hearts.Add(Instantiate(heartTemplate, heartTemplate.transform.parent));
            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].gameObject.SetActive(i < playerHealth.Max);
                hearts[i].sprite = i < playerHealth.Current ? fullHeart : emptyHeart;
            }

            if (shownMax > 0 && playerHealth.Max > shownMax)
            {
                bonusPopup.SetActive(true);
                popupUntil = Time.unscaledTime + popupTime;
            }
            shownMax = playerHealth.Max;
        }

        void Update()
        {
            if (bonusPopup.activeSelf && Time.unscaledTime >= popupUntil)
                bonusPopup.SetActive(false);
        }
    }
}
