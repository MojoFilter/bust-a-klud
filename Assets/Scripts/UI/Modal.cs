using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>
    /// A dimmed backdrop with a titled panel, a body, an optional stack of option buttons, and a row of
    /// action buttons. Pause, options, how-to-play, level-clear and game-over are all just configurations.
    /// </summary>
    public class Modal : MonoBehaviour
    {
        const float ShowTime = 0.32f;

        CanvasGroup group;
        RectTransform panel;
        Image dim;
        Text titleText;
        Text bodyText;
        RectTransform optionStack;
        RectTransform buttonRow;
        float clock = 1f;
        bool shown;
        Vector2 size;

        public bool IsShown => shown;

        public static Modal Create(Transform parent, string name, Vector2 panelSize)
        {
            RectTransform rt = UIKit.Stretch(UIKit.CreateRect(name, parent));
            var modal = rt.gameObject.AddComponent<Modal>();
            modal.Build(panelSize);
            return modal;
        }

        void Build(Vector2 panelSize)
        {
            size = panelSize;
            group = gameObject.AddComponent<CanvasGroup>();

            dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.01f, 0.05f, 0.7f);
            dim.raycastTarget = true;   // swallow clicks so the board can't be fired through a menu

            panel = UIKit.Panel(transform, "Panel", Vector2.zero, panelSize, UIKit.PanelFill, UIKit.Pumpkin);

            titleText = UIKit.Title(panel, "", 58, UIKit.Gold, UIKit.PumpkinDeep, 4f);
            UIKit.Place(titleText.rectTransform, new Vector2(0f, panelSize.y * 0.5f - 66f), new Vector2(panelSize.x - 40f, 90f));

            bodyText = UIKit.Label(panel, "", 28, UIKit.Bone, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(bodyText.rectTransform, new Vector2(0f, 10f), new Vector2(panelSize.x - 90f, panelSize.y - 260f));

            optionStack = UIKit.Place(UIKit.CreateRect("Options", panel), new Vector2(0f, 10f),
                new Vector2(panelSize.x - 90f, panelSize.y - 260f));
            var stack = optionStack.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 14f;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childControlWidth = false;
            stack.childControlHeight = false;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;

            buttonRow = UIKit.Place(UIKit.CreateRect("Buttons", panel), new Vector2(0f, -panelSize.y * 0.5f + 62f),
                new Vector2(panelSize.x - 40f, 80f));
            var row = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 24f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            gameObject.SetActive(false);
        }

        public void SetTitle(string text)
        {
            titleText.text = text;
        }

        public void SetBody(string text)
        {
            bodyText.text = text;
        }

        public Button AddButton(string label, UnityAction onClick, ButtonStyle style = ButtonStyle.Primary, float width = 230f)
        {
            return UIKit.MakeButton(buttonRow, label, Vector2.zero, new Vector2(width, 64f), onClick, style, 28);
        }

        public Button AddOption(string label, UnityAction onClick)
        {
            return UIKit.MakeButton(optionStack, label, Vector2.zero, new Vector2(400f, 58f), onClick, ButtonStyle.Secondary, 26);
        }

        public void Show()
        {
            shown = true;
            clock = 0f;
            gameObject.SetActive(true);
            group.blocksRaycasts = true;
            Apply();
        }

        public void Hide()
        {
            shown = false;
            clock = 0f;
            group.blocksRaycasts = false;
        }

        void Update()
        {
            clock += Time.unscaledDeltaTime;
            Apply();
            if (!shown && clock >= 0.15f)
                gameObject.SetActive(false);
        }

        void Apply()
        {
            if (shown)
            {
                float t = Mathf.Clamp01(clock / ShowTime);
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float u = t - 1f;
                float eased = 1f + c3 * u * u * u + c1 * u * u;
                panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, eased);
                group.alpha = Mathf.Clamp01(t * 3f);
            }
            else
            {
                float t = Mathf.Clamp01(clock / 0.15f);
                group.alpha = 1f - t;
                panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, t);
            }
        }
    }
}
