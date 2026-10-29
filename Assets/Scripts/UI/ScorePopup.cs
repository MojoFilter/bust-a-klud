using UnityEngine;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>A floating "+120" that pops in, drifts up and fades. Pooled by <see cref="UIManager"/>.</summary>
    public class ScorePopup : MonoBehaviour
    {
        const float Life = 1.15f;

        public bool Busy { get; private set; }

        RectTransform rect;
        Text label;
        float age;
        Vector2 start;
        float baseScale;

        public static ScorePopup Create(Transform parent)
        {
            Text text = UIKit.Heading(parent, "", 44, UIKit.Gold);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var popup = text.gameObject.AddComponent<ScorePopup>();
            popup.rect = text.rectTransform;
            popup.label = text;
            UIKit.Place(popup.rect, Vector2.zero, new Vector2(400f, 70f));
            text.gameObject.SetActive(false);
            return popup;
        }

        public void Play(Vector2 canvasPosition, string text, Color color, float scale)
        {
            label.text = text;
            label.color = color;
            start = canvasPosition;
            baseScale = scale;
            age = 0f;
            Busy = true;
            rect.anchoredPosition = start;
            gameObject.SetActive(true);
            Apply();
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            if (age >= Life)
            {
                Busy = false;
                gameObject.SetActive(false);
                return;
            }

            Apply();
        }

        void Apply()
        {
            float t = age / Life;
            float pop = t < 0.18f ? Mathf.Sin(t / 0.18f * Mathf.PI * 0.5f) * 1.25f : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.18f) * 4f));
            rect.localScale = Vector3.one * (baseScale * pop);
            rect.anchoredPosition = start + new Vector2(0f, 20f + t * 80f);

            Color c = label.color;
            c.a = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            label.color = c;
        }
    }
}
