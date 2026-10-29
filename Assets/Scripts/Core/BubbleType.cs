using UnityEngine;

namespace BustAKlud
{
    public enum BubbleType
    {
        None = -1,
        Bat = 0,
        Pumpkin = 1,
        Skull = 2,
        Ghost = 3,
        Spider = 4
    }

    public static class BubbleTypes
    {
        public const int Count = 5;

        static readonly string[] BallSprites = { "batball2", "pumpball2", "skullball2", "ghostball2", "spiderball2" };
        static readonly string[] BurstSprites = { "orange_bust1", "green_bust1", "blue_bust1", "black_bust1", "red_bust1" };

        // Colours used for shards, rings and score popups so each type's pop reads at a glance.
        static readonly Color[] Tints =
        {
            new Color(1.00f, 0.56f, 0.12f),
            new Color(0.40f, 0.90f, 0.22f),
            new Color(0.32f, 0.48f, 1.00f),
            new Color(0.82f, 0.84f, 0.98f),
            new Color(0.95f, 0.22f, 0.20f)
        };

        public static string BallSprite(this BubbleType type) => BallSprites[(int)type];
        public static string BurstSprite(this BubbleType type) => BurstSprites[(int)type];
        public static Color Tint(this BubbleType type) => Tints[(int)type];
    }
}
