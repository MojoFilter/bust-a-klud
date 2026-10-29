using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BustAKlud
{
    /// <summary>
    /// Owns the screen-space canvas: title, HUD, the pause/options/how-to/results modals, the level banner,
    /// score popups and the fade-to-black used between screens.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public event Action PlayClicked;
        public event Action QuitGameClicked;
        public event Action PauseClicked;
        public event Action ResumeClicked;
        public event Action QuitToTitleClicked;
        public event Action RetryClicked;
        public event Action NextLevelClicked;

        Camera cam;
        RectTransform root;
        TitleView title;
        HudView hud;
        Modal pauseModal;
        Modal optionsModal;
        Modal howToModal;
        Modal clearModal;
        Modal overModal;
        Modal optionsReturnTo;
        Button musicOption;
        Button sfxOption;
        Button guideOption;
        CanvasGroup fade;
        CanvasGroup bannerGroup;
        RectTransform bannerRect;
        Text bannerTop;
        Text bannerBottom;
        Coroutine bannerRoutine;
        readonly List<ScorePopup> popups = new List<ScorePopup>();

        public HudView Hud => hud;
        public bool AnyModalShown =>
            pauseModal.IsShown || optionsModal.IsShown || howToModal.IsShown || clearModal.IsShown || overModal.IsShown;

        public void Build(Camera worldCamera, CameraRig rig)
        {
            cam = worldCamera;

            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            rig.BindScaler(canvasGo.GetComponent<CanvasScaler>());
            root = (RectTransform)canvasGo.transform;

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            title = TitleView.Create(root);
            title.PlayClicked += () => PlayClicked?.Invoke();
            title.HowToClicked += () => howToModal.Show();
            title.OptionsClicked += () => OpenOptions(null);
            title.QuitClicked += () => QuitGameClicked?.Invoke();

            hud = HudView.Create(root);
            hud.PauseClicked += () => PauseClicked?.Invoke();
            hud.gameObject.SetActive(false);

            BuildBanner();
            BuildModals();

            var fadeRect = UIKit.Stretch(UIKit.CreateRect("Fade", root));
            var fadeImage = fadeRect.gameObject.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
            fade = fadeRect.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;

            GameSettings.Changed += RefreshOptionLabels;
            RefreshOptionLabels();
        }

        void OnDestroy()
        {
            GameSettings.Changed -= RefreshOptionLabels;
        }

        void BuildBanner()
        {
            bannerRect = UIKit.Place(UIKit.CreateRect("Banner", root), new Vector2(0f, 30f), new Vector2(1000f, 240f));
            bannerGroup = bannerRect.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerGroup.blocksRaycasts = false;

            bannerTop = UIKit.Title(bannerRect, "", 104, UIKit.Gold, UIKit.PumpkinDeep, 8f, "BannerTop");
            UIKit.Place(bannerTop.rectTransform, new Vector2(0f, 40f), new Vector2(1000f, 130f));
            bannerBottom = UIKit.Heading(bannerRect, "", 46, UIKit.Slime);
            UIKit.Place(bannerBottom.rectTransform, new Vector2(0f, -56f), new Vector2(1000f, 60f));
        }

        void BuildModals()
        {
            pauseModal = Modal.Create(root, "PauseModal", new Vector2(700f, 400f));
            pauseModal.SetTitle("PAUSED");
            pauseModal.SetBody("The ghouls will wait...\nbut not for long.");
            pauseModal.AddButton("RESUME", () => ResumeClicked?.Invoke(), ButtonStyle.Primary, 190f);
            pauseModal.AddButton("OPTIONS", () => OpenOptions(pauseModal), ButtonStyle.Secondary, 190f);
            pauseModal.AddButton("TITLE", () => QuitToTitleClicked?.Invoke(), ButtonStyle.Secondary, 190f);

            optionsModal = Modal.Create(root, "OptionsModal", new Vector2(560f, 520f));
            optionsModal.SetTitle("OPTIONS");
            musicOption = optionsModal.AddOption("", () => GameSettings.MusicOn = !GameSettings.MusicOn);
            sfxOption = optionsModal.AddOption("", () => GameSettings.SfxOn = !GameSettings.SfxOn);
            guideOption = optionsModal.AddOption("", () => GameSettings.AimGuide = !GameSettings.AimGuide);
            optionsModal.AddButton("BACK", CloseOptions, ButtonStyle.Primary, 200f);

            howToModal = Modal.Create(root, "HowToModal", new Vector2(800f, 600f));
            howToModal.SetTitle("HOW TO PLAY");
            howToModal.SetBody(
                "<color=#9CFF66>Aim</color> with the mouse (or A / D) and <color=#9CFF66>click</color> (or Space) to fire.\n\n" +
                "Match <color=#FFDB59>3 or more</color> of the same bubble to pop them.\n\n" +
                "Bubbles left hanging with no link to the ceiling <color=#FFDB59>fall</color> for bonus points.\n\n" +
                "<color=#9CFF66>Right-click</color> (or S) swaps with the waiting bubble.\n\n" +
                "Every few shots, hit or miss, the <color=#FF6B6B>ceiling creeps down</color>. Watch the skulls on the right, and don't let the bubbles reach the red line!");
            howToModal.AddButton("GOT IT", () => howToModal.Hide(), ButtonStyle.Primary, 220f);

            clearModal = Modal.Create(root, "ClearModal", new Vector2(640f, 440f));
            clearModal.SetTitle("LEVEL CLEARED!");
            clearModal.AddButton("NEXT LEVEL", () => NextLevelClicked?.Invoke(), ButtonStyle.Primary, 280f);

            overModal = Modal.Create(root, "OverModal", new Vector2(640f, 460f));
            overModal.SetTitle("GAME OVER");
            overModal.AddButton("TRY AGAIN", () => RetryClicked?.Invoke(), ButtonStyle.Primary, 240f);
            overModal.AddButton("TITLE", () => QuitToTitleClicked?.Invoke(), ButtonStyle.Secondary, 190f);
        }

        void OpenOptions(Modal returnTo)
        {
            optionsReturnTo = returnTo;
            if (returnTo != null)
                returnTo.Hide();
            optionsModal.Show();
        }

        void CloseOptions()
        {
            optionsModal.Hide();
            if (optionsReturnTo != null)
                optionsReturnTo.Show();
            optionsReturnTo = null;
        }

        void RefreshOptionLabels()
        {
            if (musicOption == null)
                return;
            UIKit.SetButtonLabel(musicOption, "MUSIC:  " + (GameSettings.MusicOn ? "ON" : "OFF"));
            UIKit.SetButtonLabel(sfxOption, "SOUND FX:  " + (GameSettings.SfxOn ? "ON" : "OFF"));
            UIKit.SetButtonLabel(guideOption, "AIM GUIDE:  " + (GameSettings.AimGuide ? "ON" : "OFF"));
        }

        // ------------------------------------------------------------------ screens

        public void ShowTitle()
        {
            HideAllModals();
            hud.gameObject.SetActive(false);
            title.gameObject.SetActive(true);
        }

        public void ShowHud()
        {
            HideAllModals();
            title.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
        }

        public void HideAllModals()
        {
            pauseModal.Hide();
            optionsModal.Hide();
            howToModal.Hide();
            clearModal.Hide();
            overModal.Hide();
            optionsReturnTo = null;
        }

        public void ShowPause() => pauseModal.Show();
        public void HidePause() => pauseModal.Hide();

        public void ShowLevelClear(int level, int bonus, int score)
        {
            clearModal.SetBody($"Level {level} complete!\n<color=#9CFF66>Bonus  +{bonus:N0}</color>\nScore  {score:N0}");
            clearModal.Show();
        }

        public void ShowGameOver(int score, int best, bool newBest)
        {
            string bestLine = newBest ? "<color=#FFDB59>NEW BEST SCORE!</color>" : $"Best  {best:N0}";
            overModal.SetBody($"The ghouls got you!\nScore  {score:N0}\n{bestLine}");
            overModal.Show();
        }

        // ------------------------------------------------------------------ transient effects

        public void ShowBanner(string top, string bottom)
        {
            if (bannerRoutine != null)
                StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(BannerRoutine(top, bottom));
        }

        IEnumerator BannerRoutine(string top, string bottom)
        {
            bannerTop.text = top;
            bannerBottom.text = bottom;
            const float inTime = 0.35f;
            const float hold = 1.3f;
            const float outTime = 0.4f;

            for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
            {
                float k = t / inTime;
                const float c1 = 1.70158f;
                float u = k - 1f;
                float eased = 1f + (c1 + 1f) * u * u * u + c1 * u * u;
                bannerRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, eased);
                bannerGroup.alpha = Mathf.Clamp01(k * 2f);
                yield return null;
            }

            bannerRect.localScale = Vector3.one;
            bannerGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);

            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
            {
                float k = t / outTime;
                bannerGroup.alpha = 1f - k;
                bannerRect.localScale = Vector3.one * (1f + k * 0.25f);
                yield return null;
            }

            bannerGroup.alpha = 0f;
            bannerRoutine = null;
        }

        /// <summary>Fades to black, runs <paramref name="atBlack"/>, then fades back in.</summary>
        public Coroutine Transition(Action atBlack, float outTime = 0.28f, float inTime = 0.4f)
        {
            return StartCoroutine(TransitionRoutine(atBlack, outTime, inTime));
        }

        IEnumerator TransitionRoutine(Action atBlack, float outTime, float inTime)
        {
            fade.blocksRaycasts = true;
            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
            {
                fade.alpha = t / outTime;
                yield return null;
            }

            fade.alpha = 1f;
            atBlack?.Invoke();
            yield return null;

            for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
            {
                fade.alpha = 1f - t / inTime;
                yield return null;
            }

            fade.alpha = 0f;
            fade.blocksRaycasts = false;
        }

        public Vector2 WorldToCanvas(Vector2 world)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
            return local;
        }

        public void Popup(Vector2 worldPosition, string text, Color color, float scale = 1f)
        {
            ScorePopup popup = null;
            for (int i = 0; i < popups.Count; i++)
            {
                if (!popups[i].Busy)
                {
                    popup = popups[i];
                    break;
                }
            }

            if (popup == null)
            {
                popup = ScorePopup.Create(root);
                popups.Add(popup);
            }

            popup.transform.SetAsLastSibling();
            // Keep popups beneath the fade and modals but above the HUD.
            popup.Play(WorldToCanvas(worldPosition), text, color, scale);
            fade.transform.SetAsLastSibling();
        }
    }
}
