using System;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Small generated textures (glows, rings, panels, stone) so the game needs no art beyond the supplied
    /// bubbles, cannon and board.
    /// </summary>
    public static class ProceduralSprites
    {
        static Sprite circle, glow, ring, star, vignette, panelFill, panelOutline, fog, white, stone, ceilingEdge, dash;

        public static Sprite White => white ??= Build(4, 4, (x, y) => new Color32(255, 255, 255, 255), 4f, FilterMode.Point);
        public static Sprite Circle => circle ??= MakeCircle();
        public static Sprite Glow => glow ??= MakeGlow();
        public static Sprite Ring => ring ??= MakeRing();
        public static Sprite Star => star ??= MakeStar();
        public static Sprite Vignette => vignette ??= MakeVignette();
        public static Sprite PanelFill => panelFill ??= MakePanel(false);
        public static Sprite PanelOutline => panelOutline ??= MakePanel(true);
        public static Sprite Fog => fog ??= MakeFog();
        public static Sprite Stone => stone ??= MakeStone();
        public static Sprite CeilingEdge => ceilingEdge ??= MakeCeilingEdge();
        public static Sprite Dash => dash ??= MakeDash();

        static Sprite Build(int w, int h, Func<int, int, Color32> pixel, float ppu = 100f,
            FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrap = TextureWrapMode.Clamp,
            Vector2? pivot = null, Vector4? border = null)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = wrap
            };

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = pixel(x, y);
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot ?? new Vector2(0.5f, 0.5f), ppu, 0,
                SpriteMeshType.FullRect, border ?? Vector4.zero);
        }

        static Color32 WhiteA(float alpha) => new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));

        static Sprite MakeCircle()
        {
            const int size = 64;
            return Build(size, size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                return WhiteA(size / 2f - d);
            });
        }

        static Sprite MakeGlow()
        {
            const int size = 128;
            return Build(size, size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                return WhiteA(a * a * (3f - 2f * a));
            });
        }

        static Sprite MakeRing()
        {
            const int size = 128;
            return Build(size, size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float band = 1f - Mathf.Abs(d - 0.82f) / 0.14f;
                return WhiteA(band);
            });
        }

        // A four-pointed twinkle: the inside of an astroid.
        static Sprite MakeStar()
        {
            const int size = 96;
            return Build(size, size, (x, y) =>
            {
                float nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                float ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                float v = 1f - (Mathf.Sqrt(nx) + Mathf.Sqrt(ny));
                return WhiteA(v * 2.2f);
            });
        }

        static Sprite MakeVignette()
        {
            const int size = 128;
            return Build(size, size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2((x + 0.5f) / size, (y + 0.5f) / size), new Vector2(0.5f, 0.5f)) * 2f;
                float t = Mathf.Clamp01((d - 0.6f) / 0.85f);
                float a = t * t * (3f - 2f * t);   // classic smoothstep: clear in the middle, dark at the corners
                return new Color32(8, 4, 16, (byte)(a * 200f));
            });
        }

        // Nine-sliceable rounded rectangle, either filled or just an outline.
        static Sprite MakePanel(bool outlineOnly)
        {
            const int size = 64;
            const float radius = 20f;
            const float thickness = 4f;
            return Build(size, size, (x, y) =>
            {
                float px = Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius);
                float py = Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius);
                float outside = Vector2.Distance(new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)), Vector2.zero);
                float inside = Mathf.Min(Mathf.Max(px, py), 0f);
                float sd = outside + inside - radius;   // negative inside the shape
                float fill = Mathf.Clamp01(0.5f - sd);
                if (!outlineOnly)
                    return WhiteA(fill);

                float inner = Mathf.Clamp01(0.5f - (sd + thickness));
                return WhiteA(fill - inner);
            }, 100f, FilterMode.Bilinear, TextureWrapMode.Clamp, null, new Vector4(radius, radius, radius, radius));
        }

        static Sprite MakeFog()
        {
            const int w = 256;
            const int h = 128;
            return Build(w, h, (x, y) =>
            {
                float u = x / (float)w;
                float v = y / (float)h;
                float n = Mathf.PerlinNoise(u * 3f + 11f, v * 3f + 5f) * 0.6f
                          + Mathf.PerlinNoise(u * 7f + 3f, v * 7f + 19f) * 0.4f;
                float falloff = Mathf.Sin(v * Mathf.PI) * Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                return new Color32(205, 200, 240, (byte)(Mathf.Clamp01((n - 0.25f) * 2.2f) * falloff * 255f));
            });
        }

        // A tileable block of dark purple masonry for the ceiling slab.
        static Sprite MakeStone()
        {
            const int size = 64;
            return Build(size, size, (x, y) =>
            {
                int row = y / 16;
                int offset = (row & 1) * 16;
                int bx = (x + offset) % 32;
                bool mortar = y % 16 == 0 || bx == 0;
                float grain = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                float shade = mortar ? 0.55f : 0.9f + grain * 0.35f;
                return new Color32((byte)(46 * shade), (byte)(30 * shade), (byte)(70 * shade), 255);
            }, 100f, FilterMode.Bilinear, TextureWrapMode.Repeat);
        }

        // The lip of the ceiling: a slab edge with little stalactites hanging below the pivot line.
        static Sprite MakeCeilingEdge()
        {
            const int w = 480;
            const int slab = 10;
            const int teeth = 18;
            const int h = slab + teeth;
            var rng = new System.Random(1313);
            int[] toothHeight = new int[w / 20 + 2];
            for (int i = 0; i < toothHeight.Length; i++)
                toothHeight[i] = 7 + rng.Next(teeth - 6);

            return Build(w, h, (x, y) =>
            {
                int fromTop = h - 1 - y;
                if (fromTop < slab)
                {
                    float shade = fromTop >= slab - 2 ? 0.6f : fromTop < 2 ? 1.25f : 1f;
                    return new Color32((byte)(Mathf.Min(255f, 58 * shade)), (byte)(36 * shade), (byte)(88 * shade), 255);
                }

                int tooth = x / 20;
                float local = (x % 20) / 20f;
                float profile = 1f - Mathf.Abs(local - 0.5f) * 2f;
                int length = Mathf.RoundToInt(toothHeight[tooth] * profile);
                bool solid = fromTop - slab < length;
                return solid ? new Color32(44, 27, 68, 255) : new Color32(0, 0, 0, 0);
            }, 100f, FilterMode.Bilinear, TextureWrapMode.Clamp, new Vector2(0.5f, teeth / (float)h));
        }

        static Sprite MakeDash()
        {
            const int w = 24;
            const int h = 8;
            return Build(w, h, (x, y) =>
            {
                float dx = Mathf.Clamp01(Mathf.Min(x - 3f, 19f - x) + 0.5f);
                float dy = Mathf.Clamp01(Mathf.Min(y - 1f, 6f - y) + 0.5f);
                return WhiteA(dx * dy);
            }, 100f, FilterMode.Bilinear, TextureWrapMode.Repeat);
        }
    }
}
