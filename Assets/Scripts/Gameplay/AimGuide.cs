using UnityEngine;

namespace BustAKlud
{
    /// <summary>Draws the marching dotted trajectory and a ghost ring where the bubble will land.</summary>
    public class AimGuide : MonoBehaviour
    {
        const int DotCount = 90;
        const float Spacing = 0.34f;
        const float SkipDistance = 0.95f;   // the first stretch is hidden inside the cannon

        SpriteRenderer[] dots;
        SpriteRenderer landingRing;
        ShotPath path;
        Vector2 landingCenter;
        Color tint = Color.white;
        bool visible;

        void Awake()
        {
            dots = new SpriteRenderer[DotCount];
            for (int i = 0; i < DotCount; i++)
            {
                var go = new GameObject("Dot");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSprites.Circle;
                sr.sortingOrder = GameConfig.OrderBubble - 1;
                go.SetActive(false);
                dots[i] = sr;
            }

            var ringGo = new GameObject("LandingRing");
            ringGo.transform.SetParent(transform, false);
            landingRing = ringGo.AddComponent<SpriteRenderer>();
            landingRing.sprite = ProceduralSprites.Ring;
            landingRing.sortingOrder = GameConfig.OrderBubble - 1;
            landingRing.gameObject.SetActive(false);
        }

        public void Show(ShotPath shotPath, Vector2 landingCenter, Color color)
        {
            path = shotPath;
            this.landingCenter = landingCenter;
            tint = color;
            visible = true;
        }

        public void Hide()
        {
            visible = false;
            for (int i = 0; i < dots.Length; i++)
                dots[i].gameObject.SetActive(false);
            landingRing.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (!visible || path == null || path.Points.Count < 2)
                return;

            float length = path.Length;
            float march = (Time.time * 0.55f) % Spacing;
            float distance = SkipDistance + march;
            int used = 0;

            while (distance < length - 0.15f && used < DotCount)
            {
                SpriteRenderer dot = dots[used++];
                dot.gameObject.SetActive(true);
                dot.transform.position = path.PointAt(distance);

                float fade = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01((distance - SkipDistance) / 16f));
                float scale = 0.15f + 0.03f * Mathf.Sin(distance * 4f - Time.time * 5f);   // sprite is 0.64 units wide
                dot.transform.localScale = Vector3.one * scale;
                dot.color = new Color(tint.r, tint.g, tint.b, 0.85f * fade);
                distance += Spacing;
            }

            for (int i = used; i < dots.Length; i++)
                dots[i].gameObject.SetActive(false);

            landingRing.gameObject.SetActive(true);
            landingRing.transform.position = landingCenter;
            float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 6f);
            landingRing.transform.localScale = Vector3.one * (GameConfig.BubbleRadius / (0.82f * 0.64f) * pulse);
            landingRing.color = new Color(tint.r, tint.g, tint.b, 0.7f);
        }
    }
}
