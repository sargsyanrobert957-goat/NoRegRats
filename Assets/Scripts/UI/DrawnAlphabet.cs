using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// The hand-drawn capitals (A-Z, 0-9 and "/") that DrawnText writes with. Each glyph is a white
    /// sprite (tinted where it is used) drawn with its capitals <see cref="CapPixels"/> tall, sitting
    /// on the bottom of the sprite. Made by StartWallSetup from Assets/Art/UI/Alphabet/.
    /// </summary>
    [CreateAssetMenu(menuName = "NoRegRats/Drawn Alphabet")]
    public class DrawnAlphabet : ScriptableObject
    {
        [Tooltip("The characters, in the same order as the glyphs.")]
        [SerializeField] string characters = "";
        [SerializeField] Sprite[] glyphs;
        [Tooltip("Height of a capital in the glyph pictures, in pixels.")]
        [SerializeField] float capPixels = 200f;
        [Tooltip("Width of a space, as a fraction of the capital height.")]
        [SerializeField] float spaceWidth = 0.35f;
        [Tooltip("Gap between letters, as a fraction of the capital height.")]
        [SerializeField] float letterGap = 0.12f;

        public float CapPixels => capPixels;
        public float SpaceWidth => spaceWidth;
        public float LetterGap => letterGap;

        public bool TryGet(char c, out Sprite glyph)
        {
            int index = characters.IndexOf(c);
            glyph = index >= 0 && index < glyphs.Length ? glyphs[index] : null;
            return glyph != null;
        }

        /// <summary>Whether every character can be drawn (spaces always can).</summary>
        public bool CanDraw(string text)
        {
            foreach (char c in text)
                if (c != ' ' && !TryGet(c, out _))
                    return false;
            return true;
        }
    }
}
