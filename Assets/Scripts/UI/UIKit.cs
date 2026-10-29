using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BustAKlud
{
    public enum ButtonStyle
    {
        Primary,
        Secondary
    }

    /// <summary>Factory helpers and the palette for building the uGUI interface from code.</summary>
    public static class UIKit
    {
        public static readonly Color Ink = new Color(0.07f, 0.03f, 0.12f, 1f);
        public static readonly Color PanelFill = new Color(0.13f, 0.06f, 0.22f, 0.9f);
        public static readonly Color Pumpkin = new Color(1f, 0.56f, 0.13f, 1f);
        public static readonly Color PumpkinDeep = new Color(0.86f, 0.36f, 0.05f, 1f);
        public static readonly Color Gold = new Color(1f, 0.86f, 0.35f, 1f);
        public static readonly Color Slime = new Color(0.62f, 1f, 0.4f, 1f);
        public static readonly Color Bone = new Color(0.98f, 0.94f, 0.82f, 1f);
        public static readonly Color Violet = new Color(0.4f, 0.22f, 0.62f, 1f);
        public static readonly Color Blood = new Color(0.92f, 0.22f, 0.27f, 1f);

        // Creepster has a small x-height, so display text is scaled up to match the body font's presence.
        const float DisplayScale = 1.22f;

        static Font font;
        static Font displayFont;
        static bool displayFontSearched;

        /// <summary>The readable body font (Unity's built-in font), used for paragraphs and small print.</summary>
        public static Font Font
        {
            get
            {
                if (font != null)
                    return font;

                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null)
                    font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                return font;
            }
        }

        /// <summary>
        /// The spooky display font for headings, buttons and scores. Replace Resources/Fonts/Title.ttf to restyle it;
        /// if it is missing, the body font is used everywhere.
        /// </summary>
        public static Font DisplayFont
        {
            get
            {
                if (!displayFontSearched)
                {
                    displayFontSearched = true;
                    displayFont = Resources.Load<Font>("Fonts/Title");
                }

                return displayFont != null ? displayFont : Font;
            }
        }

        static bool HasDisplayFont => DisplayFont != Font;

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Centre-anchored placement relative to the middle of the parent.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Picture(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            RectTransform rt = Place(CreateRect(name, parent), position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Color fill,
            Color? outline = null)
        {
            RectTransform rt = Place(CreateRect(name, parent), position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = ProceduralSprites.PanelFill;
            image.type = Image.Type.Sliced;
            image.color = fill;
            image.raycastTarget = false;

            if (outline.HasValue)
            {
                RectTransform border = Stretch(CreateRect("Outline", rt));
                var borderImage = border.gameObject.AddComponent<Image>();
                borderImage.sprite = ProceduralSprites.PanelOutline;
                borderImage.type = Image.Type.Sliced;
                borderImage.color = outline.Value;
                borderImage.raycastTarget = false;
            }

            return rt;
        }

        /// <param name="display">Use the spooky display font (headings, captions, scores) instead of the body font.</param>
        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold, bool outline = true,
            string name = "Label", bool display = false)
        {
            RectTransform rt = CreateRect(name, parent);
            var label = rt.gameObject.AddComponent<Text>();
            bool useDisplay = display && HasDisplayFont;
            label.font = useDisplay ? DisplayFont : Font;
            label.text = text;
            label.fontSize = useDisplay ? Mathf.RoundToInt(size * DisplayScale) : size;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = useDisplay ? FontStyle.Normal : style;   // Creepster has no bold face to synthesise from
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = true;
            label.raycastTarget = false;

            if (outline)
                AddOutline(rt.gameObject, Mathf.Max(1.5f, size / 15f));

            return label;
        }

        /// <summary>A label in the spooky display font: captions, scores, speech and subtitles.</summary>
        public static Text Heading(Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = true)
        {
            return Label(parent, text, size, color, anchor, FontStyle.Normal, outline, "Heading", true);
        }

        public static void AddOutline(GameObject target, float distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = new Color(0.06f, 0.02f, 0.11f, 0.92f);
            outline.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>A big animated title: vertical gradient, per-letter wobble, and a heavy outline.</summary>
        public static Text Title(Transform parent, string text, int size, Color top, Color bottom, float wobble = 7f,
            string name = "Title")
        {
            RectTransform rt = CreateRect(name, parent);
            var label = rt.gameObject.AddComponent<Text>();
            bool useDisplay = HasDisplayFont;
            label.font = DisplayFont;
            label.text = text;
            label.fontSize = useDisplay ? Mathf.RoundToInt(size * DisplayScale) : size;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = useDisplay ? FontStyle.Normal : FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            // Effect order matters: gradient and wobble must run before the outline duplicates the vertices.
            var gradient = rt.gameObject.AddComponent<TextGradient>();
            gradient.Top = top;
            gradient.Bottom = bottom;
            var wobbleEffect = rt.gameObject.AddComponent<WobbleText>();
            wobbleEffect.Amplitude = wobble;
            AddOutline(rt.gameObject, Mathf.Max(2f, size / 14f));
            return label;
        }

        public static Button MakeButton(Transform parent, string label, Vector2 position, Vector2 size,
            UnityAction onClick, ButtonStyle style = ButtonStyle.Primary, int fontSize = 30)
        {
            RectTransform rt = Place(CreateRect(label + " Button", parent), position, size);
            var fill = rt.gameObject.AddComponent<Image>();
            fill.sprite = ProceduralSprites.PanelFill;
            fill.type = Image.Type.Sliced;

            Color normal = style == ButtonStyle.Primary ? PumpkinDeep : Violet;
            Color hover = style == ButtonStyle.Primary ? Pumpkin : new Color(0.52f, 0.32f, 0.76f);
            Color pressed = Color.Lerp(normal, Color.black, 0.3f);
            fill.color = normal;

            RectTransform border = Stretch(CreateRect("Outline", rt));
            var borderImage = border.gameObject.AddComponent<Image>();
            borderImage.sprite = ProceduralSprites.PanelOutline;
            borderImage.type = Image.Type.Sliced;
            borderImage.color = style == ButtonStyle.Primary ? Gold : new Color(0.8f, 0.7f, 1f, 0.9f);
            borderImage.raycastTarget = false;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;

            Text text = Label(rt, label, fontSize, Bone, TextAnchor.MiddleCenter, FontStyle.Bold, true, "Label", true);
            Stretch(text.rectTransform);

            var fx = rt.gameObject.AddComponent<UIButtonFx>();
            fx.Target = fill;
            fx.Normal = normal;
            fx.Hover = hover;
            fx.Pressed = pressed;

            button.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.Play(Sfx.Click);
            });
            if (onClick != null)
                button.onClick.AddListener(onClick);
            return button;
        }

        public static void SetButtonLabel(Button button, string text)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = text;
        }
    }
}
