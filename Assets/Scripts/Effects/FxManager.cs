using System.Collections.Generic;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>Spawns the pooled sprite effects: pops, shards, smoke, dust, sparkles and rings.</summary>
    public class FxManager : MonoBehaviour
    {
        readonly List<FxSprite> pool = new List<FxSprite>(128);

        FxSprite Spawn()
        {
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].Busy)
                    return pool[i];

            var go = new GameObject("Fx");
            go.transform.SetParent(transform, false);
            var fx = go.AddComponent<FxSprite>();
            pool.Add(fx);
            return fx;
        }

        public void Play(Sprite sprite, Vector2 position, float life, float scaleFrom = 1f, float scaleTo = 1f,
            float alphaFrom = 1f, float alphaTo = 0f, Vector2 velocity = default, float gravity = 0f,
            float drag = 0f, float spin = 0f, float rotation = 0f, Color? color = null, bool additive = false,
            int order = GameConfig.OrderFx, Sprite[] frames = null)
        {
            Spawn().Play(sprite, frames, position, life, scaleFrom, scaleTo, alphaFrom, alphaTo, velocity,
                gravity, drag, spin, rotation, color ?? Color.white, additive, order);
        }

        /// <summary>The coloured splat, a flash, a ring and a spray of shards for one popped bubble.</summary>
        public void PopBubble(BubbleType type, Vector2 position)
        {
            Color tint = type.Tint();

            Play(GameAssets.Burst(type), position, 0.38f, 0.55f * GameConfig.ArtScale, 1.35f * GameConfig.ArtScale,
                1f, 0f, spin: Random.Range(-90f, 90f), rotation: Random.Range(0f, 360f), order: GameConfig.OrderFx);

            Play(ProceduralSprites.Glow, position, 0.28f, 0.5f, 1.7f, 0.9f, 0f, color: Color.Lerp(tint, Color.white, 0.4f),
                additive: true, order: GameConfig.OrderFx + 1);

            Play(ProceduralSprites.Ring, position, 0.34f, 0.25f, 1.1f, 0.9f, 0f, color: tint, additive: true,
                order: GameConfig.OrderFx + 1);

            int shards = 6;
            for (int i = 0; i < shards; i++)
            {
                float angle = (i / (float)shards + Random.Range(-0.08f, 0.08f)) * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float speed = Random.Range(1.6f, 3.4f);
                Play(ProceduralSprites.Circle, position, Random.Range(0.45f, 0.75f), Random.Range(0.1f, 0.18f), 0.02f,
                    1f, 0.4f, dir * speed, gravity: 6f, drag: 1.2f, color: Color.Lerp(tint, Color.white, Random.Range(0f, 0.5f)),
                    order: GameConfig.OrderFx + 2);
            }
        }

        /// <summary>A puff of smoke from the cannon mouth, drifting along the aim.</summary>
        public void Smoke(Vector2 position, Vector2 direction)
        {
            Play(null, position, 0.75f, 0.9f, 1.7f, 0.95f, 0f, direction * 0.7f + Vector2.up * 0.25f, drag: 1.5f,
                spin: Random.Range(-40f, 40f), frames: GameAssets.SmokeFrames, order: GameConfig.OrderFx + 3);

            Play(ProceduralSprites.Glow, position, 0.18f, 0.6f, 1.9f, 0.8f, 0f, color: new Color(1f, 0.7f, 0.3f),
                additive: true, order: GameConfig.OrderFx + 1);
        }

        public void Dust(Vector2 position, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(-1.6f, -0.4f));
                Play(ProceduralSprites.Glow, position + new Vector2(Random.Range(-0.15f, 0.15f), 0f),
                    Random.Range(0.5f, 0.9f), Random.Range(0.18f, 0.3f), Random.Range(0.45f, 0.7f), 0.5f, 0f,
                    velocity, drag: 1f, color: color, order: GameConfig.OrderFx);
            }
        }

        public void Sparkles(Vector2 position, int count, Color color, float radius = 0.6f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Play(ProceduralSprites.Star, position + offset, Random.Range(0.4f, 0.8f), 0.05f, Random.Range(0.25f, 0.5f),
                    1f, 0f, Random.insideUnitCircle * 0.5f, spin: Random.Range(-120f, 120f), rotation: Random.Range(0f, 90f),
                    color: color, additive: true, order: GameConfig.OrderFx + 4);
            }
        }

        public void Flash(Vector2 position, float scale, Color color)
        {
            Play(ProceduralSprites.Glow, position, 0.3f, scale * 0.4f, scale, 0.9f, 0f, color: color, additive: true,
                order: GameConfig.OrderFx + 1);
        }
    }
}
