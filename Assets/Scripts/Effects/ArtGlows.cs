using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Adds flickering lamplight in the mansion windows and a slow pulsing halo on the moon to the painted
    /// board art. Attach to the art's sprite object; positions are in the painting's own pixel space so the
    /// glows follow the art when it is scaled or moved.
    /// </summary>
    public class ArtGlows : MonoBehaviour
    {
        struct Glow
        {
            public SpriteRenderer Renderer;
            public float BaseAlpha;
            public float Phase;
            public float Speed;
            public bool Flicker;
        }

        // Pixel positions in gameboard1.jpg (480x640, origin top-left).
        static readonly Vector2[] Windows = { new Vector2(86f, 106f), new Vector2(104f, 105f), new Vector2(141f, 166f) };
        static readonly Vector2 Moon = new Vector2(331f, 48f);

        Glow[] glows;

        public static void Attach(GameObject art, int sortingOrder)
        {
            art.AddComponent<ArtGlows>().Build(sortingOrder);
        }

        static Vector3 ArtPosition(Vector2 pixel)
        {
            return new Vector3((pixel.x - 240f) / 100f, (320f - pixel.y) / 100f, 0f);
        }

        void Build(int sortingOrder)
        {
            glows = new Glow[Windows.Length + 1];

            for (int i = 0; i < Windows.Length; i++)
            {
                glows[i] = MakeGlow("WindowGlow" + i, Windows[i], 0.62f, new Color(1f, 0.62f, 0.2f), 0.55f, sortingOrder);
                glows[i].Phase = i * 2.3f;
                glows[i].Speed = 3.1f + i * 0.7f;
                glows[i].Flicker = true;
            }

            Glow moon = MakeGlow("MoonGlow", Moon, 2.1f, new Color(0.8f, 0.88f, 1f), 0.4f, sortingOrder);
            moon.Speed = 0.6f;
            glows[glows.Length - 1] = moon;
        }

        Glow MakeGlow(string objectName, Vector2 pixel, float scale, Color color, float alpha, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = ArtPosition(pixel);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.Glow;
            sr.sharedMaterial = FxSprite.AdditiveMaterial;
            sr.sortingOrder = sortingOrder;
            sr.color = new Color(color.r, color.g, color.b, alpha);

            return new Glow { Renderer = sr, BaseAlpha = alpha };
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < glows.Length; i++)
            {
                Glow g = glows[i];
                float level = g.Flicker
                    ? 0.65f + 0.35f * Mathf.PerlinNoise(t * g.Speed, g.Phase)
                    : 0.8f + 0.2f * Mathf.Sin(t * g.Speed);
                Color c = g.Renderer.color;
                c.a = g.BaseAlpha * level;
                g.Renderer.color = c;
            }
        }
    }
}
