using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>The title screen: wobbling logo, a swaying Blunderthor, drifting bubbles and the main menu.</summary>
    public class TitleView : MonoBehaviour
    {
        struct Floater
        {
            public RectTransform Rect;
            public Vector2 Home;
            public float Phase;
            public float Speed;
            public float Amplitude;
            public float Spin;
        }

        static readonly Vector2[] FloaterSpots =
        {
            new Vector2(540f, 262f), new Vector2(604f, 96f), new Vector2(548f, -84f), new Vector2(470f, -262f),
            new Vector2(-596f, 270f), new Vector2(-250f, 300f), new Vector2(330f, 316f), new Vector2(-70f, -318f),
            new Vector2(612f, -318f), new Vector2(-606f, -300f)
        };

        public event Action PlayClicked;
        public event Action HowToClicked;
        public event Action OptionsClicked;
        public event Action QuitClicked;

        readonly List<Floater> floaters = new List<Floater>();
        RectTransform mascot;
        Text bestText;

        public static TitleView Create(Transform parent)
        {
            RectTransform rt = UIKit.Stretch(UIKit.CreateRect("Title", parent));
            var view = rt.gameObject.AddComponent<TitleView>();
            view.Build();
            return view;
        }

        void Build()
        {
            // A gentle shade so the logo and buttons read against the painted backdrop.
            var shade = UIKit.Stretch(UIKit.CreateRect("Shade", transform)).gameObject.AddComponent<Image>();
            shade.color = new Color(0.03f, 0.01f, 0.08f, 0.38f);
            shade.raycastTarget = false;

            BuildFloaters();

            Image picture = UIKit.Picture(transform, "Blunderthor", GameAssets.Mascot, Vector2.zero, new Vector2(286f, 461f));
            mascot = picture.rectTransform;
            mascot.pivot = new Vector2(0.5f, 0f);
            mascot.anchoredPosition = new Vector2(-400f, -352f);

            Text logo = UIKit.Title(transform, "BUST-A-KLUD", 124, UIKit.Gold, UIKit.PumpkinDeep, 9f, "Logo");
            UIKit.Place(logo.rectTransform, new Vector2(70f, 208f), new Vector2(1000f, 160f));

            Text subtitle = UIKit.Heading(transform, "~  a spooky bubble bash  ~", 42, UIKit.Slime);
            UIKit.Place(subtitle.rectTransform, new Vector2(70f, 118f), new Vector2(800f, 60f));

            float x = 170f;
            UIKit.MakeButton(transform, "PLAY", new Vector2(x, 14f), new Vector2(340f, 88f), () => PlayClicked?.Invoke(),
                ButtonStyle.Primary, 48);
            UIKit.MakeButton(transform, "HOW TO PLAY", new Vector2(x, -78f), new Vector2(340f, 64f), () => HowToClicked?.Invoke(),
                ButtonStyle.Secondary, 28);
            UIKit.MakeButton(transform, "OPTIONS", new Vector2(x, -152f), new Vector2(340f, 64f), () => OptionsClicked?.Invoke(),
                ButtonStyle.Secondary, 28);

            if (Application.platform != RuntimePlatform.WebGLPlayer)
                UIKit.MakeButton(transform, "QUIT", new Vector2(x, -226f), new Vector2(340f, 64f), () => QuitClicked?.Invoke(),
                    ButtonStyle.Secondary, 28);

            bestText = UIKit.Heading(transform, "", 32, UIKit.Gold);
            UIKit.Place(bestText.rectTransform, new Vector2(70f, -318f), new Vector2(700f, 44f));
        }

        void BuildFloaters()
        {
            BubbleType[] types = { BubbleType.Bat, BubbleType.Pumpkin, BubbleType.Skull, BubbleType.Ghost, BubbleType.Spider };
            for (int i = 0; i < FloaterSpots.Length; i++)
            {
                float size = 70f + (i % 3) * 22f;
                Image bubble = UIKit.Picture(transform, "Floater" + i, GameAssets.Bubble(types[i % types.Length]),
                    FloaterSpots[i], new Vector2(size, size));
                floaters.Add(new Floater
                {
                    Rect = bubble.rectTransform,
                    Home = FloaterSpots[i],
                    Phase = i * 1.7f,
                    Speed = 0.7f + (i % 4) * 0.18f,
                    Amplitude = 12f + (i % 3) * 7f,
                    Spin = (i % 2 == 0 ? 1f : -1f) * (6f + i)
                });
            }
        }

        void OnEnable()
        {
            if (bestText != null)
                bestText.text = GameSettings.HighScore > 0 ? "BEST SCORE  " + GameSettings.HighScore.ToString("N0") : "";
        }

        void Update()
        {
            float t = Time.unscaledTime;

            for (int i = 0; i < floaters.Count; i++)
            {
                Floater f = floaters[i];
                f.Rect.anchoredPosition = f.Home + new Vector2(Mathf.Sin(t * f.Speed * 0.7f + f.Phase) * f.Amplitude * 0.6f,
                    Mathf.Sin(t * f.Speed + f.Phase) * f.Amplitude);
                f.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f + f.Phase) * f.Spin);
            }

            float sway = Mathf.Sin(t * 1.3f);
            float breathe = 1f + 0.012f * Mathf.Sin(t * 2.1f);
            mascot.localRotation = Quaternion.Euler(0f, 0f, sway * 2.5f);
            mascot.localScale = new Vector3(2f - breathe, breathe, 1f);
        }
    }
}
