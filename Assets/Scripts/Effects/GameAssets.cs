using System.Collections.Generic;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>Loads and caches the art and audio shipped under Resources.</summary>
    public static class GameAssets
    {
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        static Sprite cannonBody;
        static Sprite[] smokeFrames;

        public static Sprite Art(string name)
        {
            if (sprites.TryGetValue(name, out Sprite cached) && cached != null)
                return cached;

            var sprite = Resources.Load<Sprite>("Art/" + name);
            if (sprite == null)
                Debug.LogError($"Missing sprite 'Resources/Art/{name}'");
            sprites[name] = sprite;
            return sprite;
        }

        public static AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out AudioClip cached) && cached != null)
                return cached;

            var clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null)
                Debug.LogError($"Missing audio clip 'Resources/Audio/{name}'");
            clips[name] = clip;
            return clip;
        }

        public static Sprite Bubble(BubbleType type) => Art(type.BallSprite());
        public static Sprite Burst(BubbleType type) => Art(type.BurstSprite());
        public static Sprite Board => Art("gameboard1");
        public static Sprite Mascot => Art("Blunderthor");
        public static Sprite CannonBase => Art("cannonbase");

        /// <summary>The cannon art re-pivoted on its bulb so it rotates around the base's pivot knob.</summary>
        public static Sprite CannonBody
        {
            get
            {
                if (cannonBody != null)
                    return cannonBody;

                Sprite source = Art("cannon");
                Texture2D tex = source.texture;
                cannonBody = Sprite.Create(tex, source.textureRect, new Vector2(0.30f, 0.5f), 100f);
                return cannonBody;
            }
        }

        static Sprite cannonSkull;
        static bool cannonSkullBuilt;

        /// <summary>
        /// The skull emblem on the cannon, cut out as a white-on-transparent overlay in exactly the cannon's pose
        /// so it can be tinted to the loaded bubble's colour. Null if the art isn't readable (callers fall back).
        /// </summary>
        public static Sprite CannonSkull
        {
            get
            {
                if (!cannonSkullBuilt)
                {
                    cannonSkullBuilt = true;
                    cannonSkull = BuildSkullOverlay();
                }

                return cannonSkull;
            }
        }

        static Sprite BuildSkullOverlay()
        {
            Sprite source = Art("cannon");
            if (source == null || !source.texture.isReadable)
            {
                Debug.LogWarning("cannon.png is not readable; the loaded bubble will be drawn instead of a tinted skull.");
                return null;
            }

            Texture2D tex = source.texture;
            Rect rect = source.textureRect;
            int width = (int)rect.width;
            int height = (int)rect.height;
            Color32[] pixels = tex.GetPixels32();
            var overlay = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Only the emblem region (this keeps the cannon's soft specular highlight out of the mask).
                    bool inEmblem = x >= 30 && x <= 66 && y >= 13 && y <= 59;
                    Color32 p = pixels[((int)rect.y + y) * tex.width + (int)rect.x + x];
                    float brightness = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                    float alpha = inEmblem ? Mathf.Clamp01((brightness - 170f) / 55f) * (p.a / 255f) : 0f;
                    overlay[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            var mask = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            mask.SetPixels32(overlay);
            mask.Apply(false, true);
            return Sprite.Create(mask, new Rect(0, 0, width, height), new Vector2(0.30f, 0.5f), 100f);
        }

        /// <summary>The "swirl" reel from cannonsmoke.reelset.js, sliced out of the sprite sheet.</summary>
        public static Sprite[] SmokeFrames
        {
            get
            {
                if (smokeFrames != null && smokeFrames.Length > 0 && smokeFrames[0] != null)
                    return smokeFrames;

                Sprite sheet = Art("cannonsmoke");
                Texture2D tex = sheet.texture;
                int height = tex.height;

                // x, y (from the top), width, height, as listed in the reelset file.
                int[,] rects =
                {
                    { 4, 1, 52, 52 }, { 68, 1, 52, 52 }, { 130, 0, 52, 52 },
                    { 192, 1, 52, 52 }, { 252, 23, 16, 16 }, { 269, 23, 16, 16 }
                };

                smokeFrames = new Sprite[rects.GetLength(0)];
                for (int i = 0; i < smokeFrames.Length; i++)
                {
                    var rect = new Rect(rects[i, 0], height - rects[i, 1] - rects[i, 3], rects[i, 2], rects[i, 3]);
                    smokeFrames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                }

                return smokeFrames;
            }
        }
    }
}
