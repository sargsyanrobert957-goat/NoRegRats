using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoRegRats
{
    /// <summary>
    /// Writes a short line (a key name) in the hand-drawn capitals of a DrawnAlphabet, as one Image
    /// per letter laid out in this RectTransform. Text is shown in capitals. If it has a character
    /// the alphabet can't draw (";", "?"), the fallback Text shows it in the plain font instead.
    /// Works on screen canvases and world-space ones (the start wall).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DrawnText : MonoBehaviour
    {
        public enum Align { Left, Centre, Right }

        [SerializeField] DrawnAlphabet alphabet;
        [Tooltip("Height of a capital, in canvas units.")]
        [SerializeField] float capHeight = 40f;
        [SerializeField] Color color = new Color(0.1f, 0.1f, 0.1f);
        [SerializeField] Align alignment = Align.Centre;
        [Tooltip("Shows anything the alphabet can't draw, in the plain font. Can be empty.")]
        [SerializeField] Text fallback;

        readonly List<Image> letters = new List<Image>();
        string shown;

        /// <summary>Width of what is shown, in canvas units.</summary>
        public float Width { get; private set; }

        public void SetText(string text)
        {
            text ??= "";
            if (text == shown)
                return;
            shown = text;
            string upper = text.ToUpperInvariant();

            if (alphabet == null || !alphabet.CanDraw(upper))
            {
                ShowLetters(0);
                if (fallback != null)
                {
                    fallback.enabled = true;
                    fallback.text = text;
                    Width = fallback.preferredWidth;
                }
                else
                    Width = 0f;
                return;
            }

            if (fallback != null)
                fallback.enabled = false;
            float scale = capHeight / alphabet.CapPixels;
            float gap = capHeight * alphabet.LetterGap;

            // Measure, then place from the aligned start.
            float width = 0f;
            int count = 0;
            for (int i = 0; i < upper.Length; i++)
            {
                if (i > 0)
                    width += gap;
                if (upper[i] == ' ' || !alphabet.TryGet(upper[i], out Sprite glyph))
                    width += capHeight * alphabet.SpaceWidth;
                else
                {
                    width += glyph.rect.width * scale;
                    count++;
                }
            }
            Width = width;
            ShowLetters(count);

            var rect = (RectTransform)transform;
            float x = alignment switch
            {
                Align.Left => rect.rect.xMin,
                Align.Right => rect.rect.xMax - width,
                _ => rect.rect.center.x - width * 0.5f,
            };
            float baseline = rect.rect.center.y - capHeight * 0.5f;
            int n = 0;
            for (int i = 0; i < upper.Length; i++)
            {
                if (i > 0)
                    x += gap;
                if (upper[i] == ' ' || !alphabet.TryGet(upper[i], out Sprite glyph))
                {
                    x += capHeight * alphabet.SpaceWidth;
                    continue;
                }
                Image letter = letters[n++];
                letter.sprite = glyph;
                letter.color = color;
                var r = letter.rectTransform;
                r.sizeDelta = glyph.rect.size * scale;
                r.anchoredPosition = new Vector2(x, baseline);
                x += glyph.rect.width * scale;
            }
        }

        // Keeps exactly `count` letter images switched on, making more as needed.
        void ShowLetters(int count)
        {
            while (letters.Count < count)
            {
                var go = new GameObject("Letter", typeof(RectTransform));
                var r = (RectTransform)go.transform;
                r.SetParent(transform, false);
                // Anchored at this rect's pivot, the point its rect is measured from.
                r.anchorMin = r.anchorMax = ((RectTransform)transform).pivot;
                r.pivot = Vector2.zero;
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                letters.Add(image);
            }
            for (int i = 0; i < letters.Count; i++)
                letters[i].enabled = i < count;
        }
    }
}
