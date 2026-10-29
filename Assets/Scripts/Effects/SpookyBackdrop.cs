using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Everything behind and around the board: the painting blown up and dimmed to fill the window, drifting
    /// fog, rising wisps, the moon and window glows, and a vignette over the whole scene.
    /// </summary>
    public class SpookyBackdrop : MonoBehaviour
    {
        struct Wisp
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float X;
            public float Speed;
            public float Phase;
            public float Sway;
            public float Size;
            public float Offset;
        }

        const int WispCount = 16;
        const int FogCount = 3;

        CameraRig rig;
        Transform artTransform;
        Transform vignetteTransform;
        Transform[] fog;
        Wisp[] wisps;

        public void Init(CameraRig cameraRig)
        {
            rig = cameraRig;
            BuildArt();
            BuildFog();
            BuildWisps();
            BuildVignette();
            Fit();
        }

        void BuildArt()
        {
            var go = new GameObject("BackdropArt");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.Board;
            sr.sortingOrder = GameConfig.OrderBackdrop;
            sr.color = new Color(0.5f, 0.42f, 0.72f);
            artTransform = go.transform;
            ArtGlows.Attach(go, GameConfig.OrderBackdrop + 1);
        }

        void BuildFog()
        {
            fog = new Transform[FogCount];
            for (int i = 0; i < FogCount; i++)
            {
                var go = new GameObject("Fog" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSprites.Fog;
                sr.sortingOrder = GameConfig.OrderBackdrop + 2 + i;
                sr.color = new Color(0.78f, 0.74f, 1f, 0.2f - i * 0.03f);
                go.transform.localScale = new Vector3(6.5f + i * 1.5f, 2.4f + i * 0.6f, 1f);
                fog[i] = go.transform;
            }
        }

        void BuildWisps()
        {
            Color[] palette =
            {
                new Color(0.6f, 1f, 0.45f), new Color(1f, 0.65f, 0.25f), new Color(0.75f, 0.6f, 1f), new Color(0.6f, 0.9f, 1f)
            };

            wisps = new Wisp[WispCount];
            for (int i = 0; i < WispCount; i++)
            {
                var go = new GameObject("Wisp" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSprites.Glow;
                sr.sharedMaterial = FxSprite.AdditiveMaterial;
                sr.sortingOrder = GameConfig.OrderBackdrop + 6;
                Color c = palette[i % palette.Length];
                sr.color = new Color(c.r, c.g, c.b, 0.5f);

                wisps[i] = new Wisp
                {
                    Transform = go.transform,
                    Renderer = sr,
                    X = Random.Range(-6.4f, 6.4f),
                    Speed = Random.Range(0.12f, 0.34f),
                    Phase = Random.Range(0f, 10f),
                    Sway = Random.Range(0.2f, 0.6f),
                    Size = Random.Range(0.18f, 0.5f),
                    Offset = Random.value
                };
            }
        }

        void BuildVignette()
        {
            var go = new GameObject("Vignette");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.Vignette;
            sr.sortingOrder = GameConfig.OrderVignette;
            vignetteTransform = go.transform;
        }

        void Fit()
        {
            float w = rig.VisibleWidth;
            float h = rig.VisibleHeight;

            // Cover the window with the painting, pinned to the top so the mansion and moon stay in view.
            float scale = Mathf.Max(w / GameConfig.BoardWidth, h / GameConfig.BoardHeight) * 1.1f;
            float sway = Mathf.Sin(Time.time * 0.07f) * 0.18f;
            float breathe = 1f + 0.012f * Mathf.Sin(Time.time * 0.13f);
            artTransform.localScale = Vector3.one * (scale * breathe);
            artTransform.localPosition = new Vector3(sway, h * 0.5f - GameConfig.BoardHeight * scale * 0.5f + 0.1f, 0f);

            vignetteTransform.localScale = new Vector3(w / 1.28f * 1.02f, h / 1.28f * 1.02f, 1f);
        }

        void Update()
        {
            Fit();

            float t = Time.time;
            float w = rig.VisibleWidth;
            float h = rig.VisibleHeight;

            for (int i = 0; i < fog.Length; i++)
            {
                float x = Mathf.Sin(t * (0.05f + i * 0.02f) + i * 1.9f) * (w * 0.18f);
                float y = -h * 0.28f + i * h * 0.26f + Mathf.Sin(t * 0.11f + i) * 0.2f;
                fog[i].localPosition = new Vector3(x, y, 0f);
            }

            for (int i = 0; i < wisps.Length; i++)
            {
                Wisp wisp = wisps[i];
                float progress = Mathf.Repeat(t * wisp.Speed * 0.1f + wisp.Offset, 1f);
                float y = Mathf.Lerp(-h * 0.55f, h * 0.55f, progress);
                float x = wisp.X * (w / 12.8f) + Mathf.Sin(t * 0.6f + wisp.Phase) * wisp.Sway;
                wisp.Transform.localPosition = new Vector3(x, y, 0f);
                wisp.Transform.localScale = Vector3.one * wisp.Size * (0.8f + 0.2f * Mathf.Sin(t * 2f + wisp.Phase));

                Color c = wisp.Renderer.color;
                c.a = Mathf.Sin(progress * Mathf.PI) * 0.55f;
                wisp.Renderer.color = c;
            }
        }
    }
}
