using UnityEngine;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    /// <summary>Shared visual tokens and surfaces for the runtime uGUI.</summary>
    public static class PhasebreakUiTheme
    {
        public static readonly Color Window = new(.035f, .037f, .041f, .96f);
        public static readonly Color Panel = new(.067f, .068f, .071f, .94f);
        public static readonly Color Raised = new(.12f, .12f, .12f, .98f);
        public static readonly Color MetalEdge = new(.38f, .36f, .32f, .7f);
        public static readonly Color Accent = new(.62f, .52f, .36f, 1f);
        public static readonly Color Text = new(.93f, .9f, .83f, 1f);
        public static readonly Color MutedText = new(.66f, .65f, .61f, 1f);
        public static readonly Color Hover = new(.27f, .26f, .24f, 1f);
        public static readonly Color Active = new(.38f, .32f, .23f, 1f);

        private static Sprite roundedSprite;

        public static void StyleSurface(Image image, Color fill, bool framed = true)
        {
            if (image == null) return;
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;
            if (!framed) return;
            Outline outline = image.GetComponent<Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = MetalEdge;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        public static void StyleButton(Button button)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            StyleSurface(image, Raised);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.13f, 1.03f, 1f);
            colors.pressedColor = new Color(.76f, .72f, .65f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(.55f, .55f, .55f, .6f);
            button.colors = colors;
        }

        private static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite != null) return roundedSprite;
                const int size = 32;
                const float radius = 7f;
                Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
                {
                    name = "PHASEBREAK Rounded UI Mask",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 q = new(Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius),
                        Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius));
                    float distance = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                        + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(.5f - distance));
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                    new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(9f, 9f, 9f, 9f));
                roundedSprite.name = "PHASEBREAK Rounded UI";
                roundedSprite.hideFlags = HideFlags.DontSave;
                return roundedSprite;
            }
        }
    }
}
