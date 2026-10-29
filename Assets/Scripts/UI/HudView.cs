using System;
using UnityEngine;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>
    /// The in-game side panels: Blunderthor and his speech bubble and the score on the left; level, the
    /// ceiling-drop meter, controls and quick toggles on the right.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        const float LeftX = -445f;
        const float RightX = 445f;
        const float PipSize = 32f;
        const float PipSpacing = 38f;
        const float PipRowWidth = 290f;   // room inside the ceiling panel
        static readonly Vector2 MascotHome = new Vector2(LeftX, 14f);

        public event Action PauseClicked;

        Text scoreText;
        Text bestText;
        Text levelText;
        Text levelNameText;
        RectTransform pipRow;
        Image[] pips = new Image[0];
        RectTransform mascot;
        RectTransform bubble;
        Text bubbleText;
        Button musicButton;
        Button sfxButton;
        Button guideButton;

        int shownScore;
        int targetScore;
        int pipsLit;
        float bubbleTimer;
        Spring mascotHop;
        Spring mascotSquash = new Spring(1f);
        Spring bubbleScale;
        Spring scorePunch = new Spring(1f);

        public static HudView Create(Transform parent)
        {
            RectTransform rt = UIKit.Stretch(UIKit.CreateRect("HUD", parent));
            var hud = rt.gameObject.AddComponent<HudView>();
            hud.Build();
            return hud;
        }

        void Build()
        {
            BuildLeft();
            BuildRight();
            GameSettings.Changed += RefreshToggles;
            RefreshToggles();
        }

        void OnDestroy()
        {
            GameSettings.Changed -= RefreshToggles;
        }

        void BuildLeft()
        {
            Text logo = UIKit.Title(transform, "BUST-A-KLUD", 42, UIKit.Gold, UIKit.PumpkinDeep, 3f, "MiniLogo");
            UIKit.Place(logo.rectTransform, new Vector2(LeftX, 322f), new Vector2(340f, 60f));

            Image picture = UIKit.Picture(transform, "Blunderthor", GameAssets.Mascot, MascotHome, new Vector2(167f, 269f));
            mascot = picture.rectTransform;
            mascot.pivot = new Vector2(0.5f, 0f);
            mascot.anchoredPosition = new Vector2(LeftX, MascotHome.y - 134f);

            // Speech bubble above his head, with a tail pointing down at him.
            bubble = UIKit.Panel(transform, "SpeechBubble", new Vector2(LeftX, 222f), new Vector2(312f, 112f), UIKit.Bone);
            RectTransform tail = UIKit.Panel(bubble, "Tail", new Vector2(-48f, -58f), new Vector2(30f, 30f), UIKit.Bone);
            tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.SetAsFirstSibling();
            bubbleText = UIKit.Heading(bubble, "", 26, UIKit.Ink, TextAnchor.MiddleCenter, false);
            UIKit.Stretch(bubbleText.rectTransform, 14f);
            bubble.localScale = Vector3.zero;

            RectTransform scorePanel = UIKit.Panel(transform, "ScorePanel", new Vector2(LeftX, -236f), new Vector2(320f, 170f),
                UIKit.PanelFill, UIKit.Pumpkin);
            Text scoreCaption = UIKit.Heading(scorePanel, "SCORE", 26, UIKit.Slime);
            UIKit.Place(scoreCaption.rectTransform, new Vector2(0f, 52f), new Vector2(280f, 34f));
            scoreText = UIKit.Heading(scorePanel, "0", 58, UIKit.Gold);
            UIKit.Place(scoreText.rectTransform, new Vector2(0f, 6f), new Vector2(300f, 74f));
            bestText = UIKit.Heading(scorePanel, "BEST 0", 26, UIKit.Bone);
            UIKit.Place(bestText.rectTransform, new Vector2(0f, -50f), new Vector2(280f, 34f));
        }

        void BuildRight()
        {
            RectTransform levelPanel = UIKit.Panel(transform, "LevelPanel", new Vector2(RightX, 236f), new Vector2(330f, 152f),
                UIKit.PanelFill, UIKit.Pumpkin);
            levelText = UIKit.Title(levelPanel, "LEVEL 1", 56, UIKit.Gold, UIKit.PumpkinDeep, 4f);
            UIKit.Place(levelText.rectTransform, new Vector2(0f, 24f), new Vector2(310f, 76f));
            levelNameText = UIKit.Heading(levelPanel, "", 28, UIKit.Slime);
            UIKit.Place(levelNameText.rectTransform, new Vector2(0f, -40f), new Vector2(310f, 40f));

            RectTransform ceilingPanel = UIKit.Panel(transform, "CeilingPanel", new Vector2(RightX, 60f), new Vector2(330f, 120f),
                UIKit.PanelFill, UIKit.Pumpkin);
            Text ceilingCaption = UIKit.Heading(ceilingPanel, "CEILING DROPS IN", 23, UIKit.Slime);
            UIKit.Place(ceilingCaption.rectTransform, new Vector2(0f, 36f), new Vector2(300f, 30f));
            pipRow = UIKit.Place(UIKit.CreateRect("Pips", ceilingPanel), new Vector2(0f, -8f), new Vector2(300f, PipSize + 8f));
            Text missCaption = UIKit.Heading(ceilingPanel, "SHOTS", 19, UIKit.Bone);
            UIKit.Place(missCaption.rectTransform, new Vector2(0f, -42f), new Vector2(300f, 24f));

            RectTransform helpPanel = UIKit.Panel(transform, "HelpPanel", new Vector2(RightX, -112f), new Vector2(330f, 132f),
                new Color(0.1f, 0.05f, 0.18f, 0.8f), new Color(1f, 0.56f, 0.13f, 0.5f));
            Text help = UIKit.Label(helpPanel,
                "<color=#9CFF66>AIM</color>   mouse  or  A / D\n<color=#9CFF66>FIRE</color>   click  or  Space\n<color=#9CFF66>SWAP</color>   right-click  or  S",
                21, UIKit.Bone, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            UIKit.Place(help.rectTransform, new Vector2(8f, 0f), new Vector2(290f, 110f));

            Vector2 size = new Vector2(158f, 48f);
            UIKit.MakeButton(transform, "PAUSE", new Vector2(RightX - 84f, -238f), size, () => PauseClicked?.Invoke(),
                ButtonStyle.Primary, 22);
            musicButton = UIKit.MakeButton(transform, "MUSIC", new Vector2(RightX + 84f, -238f), size,
                () => GameSettings.MusicOn = !GameSettings.MusicOn, ButtonStyle.Secondary, 20);
            sfxButton = UIKit.MakeButton(transform, "SFX", new Vector2(RightX - 84f, -296f), size,
                () => GameSettings.SfxOn = !GameSettings.SfxOn, ButtonStyle.Secondary, 20);
            guideButton = UIKit.MakeButton(transform, "AIM", new Vector2(RightX + 84f, -296f), size,
                () => GameSettings.AimGuide = !GameSettings.AimGuide, ButtonStyle.Secondary, 20);
        }

        void RefreshToggles()
        {
            if (musicButton == null)
                return;
            UIKit.SetButtonLabel(musicButton, "MUSIC " + (GameSettings.MusicOn ? "ON" : "OFF"));
            UIKit.SetButtonLabel(sfxButton, "SFX " + (GameSettings.SfxOn ? "ON" : "OFF"));
            UIKit.SetButtonLabel(guideButton, "GUIDE " + (GameSettings.AimGuide ? "ON" : "OFF"));
        }

        public void SetScore(int score, bool snap = false)
        {
            if (score > targetScore)
                scorePunch.Velocity = 5f;
            targetScore = score;
            if (snap)
                shownScore = score;
        }

        public void SetBest(int best)
        {
            bestText.text = "BEST " + best.ToString("N0");
        }

        public void SetLevel(int level, string levelName)
        {
            levelText.text = "LEVEL " + level;
            levelNameText.text = levelName;
        }

        /// <summary>Shows how many more shots remain before the ceiling drops.</summary>
        public void SetCeiling(int remaining, int limit)
        {
            if (pips.Length != limit)
                RebuildPips(limit);
            pipsLit = Mathf.Clamp(remaining, 0, limit);
        }

        void RebuildPips(int count)
        {
            for (int i = pipRow.childCount - 1; i >= 0; i--)
                Destroy(pipRow.GetChild(i).gameObject);

            // Shrink the row to fit the panel when there are many shots between drops.
            float spacing = Mathf.Min(PipSpacing, PipRowWidth / count);
            float size = Mathf.Min(PipSize, spacing - 3f);

            pips = new Image[count];
            float start = -(count - 1) * spacing * 0.5f;
            for (int i = 0; i < count; i++)
                pips[i] = UIKit.Picture(pipRow, "Pip" + i, GameAssets.Bubble(BubbleType.Skull),
                    new Vector2(start + i * spacing, 0f), new Vector2(size, size));
        }

        /// <summary>Blunderthor hops and says something.</summary>
        public void Say(string line, float seconds = 2.6f)
        {
            bubbleText.text = line;
            bubbleTimer = seconds;
            mascotHop.Velocity = 260f;
            mascotSquash.Velocity = -3f;
        }

        public void Hop(float strength = 1f)
        {
            mascotHop.Velocity = 220f * strength;
            mascotSquash.Velocity = -2.5f * strength;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            if (shownScore != targetScore)
            {
                int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(targetScore - shownScore) * 6f * dt));
                shownScore = (int)Mathf.MoveTowards(shownScore, targetScore, step);
                scoreText.text = shownScore.ToString("N0");
            }

            scorePunch.Step(1f, dt, 260f, 12f);
            scoreText.rectTransform.localScale = Vector3.one * scorePunch.Value;

            float t = Time.unscaledTime;
            mascotHop.Step(0f, dt, 260f, 11f);
            mascotSquash.Step(1f, dt, 300f, 12f);
            mascot.anchoredPosition = new Vector2(LeftX, MascotHome.y - 134f + Mathf.Sin(t * 1.7f) * 4f + mascotHop.Value);
            mascot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f) * 2.2f);
            mascot.localScale = new Vector3(2f - mascotSquash.Value, mascotSquash.Value, 1f);

            bubbleTimer -= dt;
            bubbleScale.Step(bubbleTimer > 0f ? 1f : 0f, dt, 380f, 17f);
            float s = Mathf.Max(0f, bubbleScale.Value);
            bubble.localScale = new Vector3(s, s, 1f);

            float blink = 0.5f + 0.5f * Mathf.Sin(t * 9f);
            for (int i = 0; i < pips.Length; i++)
            {
                bool lit = i < pipsLit;
                Color color = lit ? Color.white : new Color(0.16f, 0.12f, 0.24f, 0.75f);
                if (lit && pipsLit == 1)
                    color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), blink);
                pips[i].color = color;
                pips[i].rectTransform.localScale = Vector3.one * (lit && pipsLit == 1 ? 1f + 0.12f * blink : 1f);
            }
        }
    }
}
