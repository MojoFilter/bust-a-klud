using UnityEngine;

namespace BustAKlud
{
    /// <summary>
    /// Board geometry and tuning values. World units: 100 art pixels = 1 unit, board centred on the origin.
    /// </summary>
    public static class GameConfig
    {
        public const int Columns = 8;
        public const int MaxRows = 10;        // a bubble resting in row >= MaxRows (counting ceiling drops) ends the game
        public const int StorageRows = 16;    // rows the logic grid can hold (headroom below the lose line)

        public const float BubbleDiameter = 0.6f;
        public const float BubbleRadius = BubbleDiameter * 0.5f;
        public const float RowHeight = BubbleDiameter * 0.8660254f;
        public const float ArtScale = BubbleDiameter / 0.64f;   // the ball art is 64px

        public const float BoardLeft = -2.4f;
        public const float BoardRight = 2.4f;
        public const float BoardTop = 3.2f;
        public const float BoardBottom = -3.2f;
        public const float BoardWidth = BoardRight - BoardLeft;
        public const float BoardHeight = BoardTop - BoardBottom;

        public const float CeilingThickness = 0.1f;
        public const float CeilingBaseY = BoardTop - CeilingThickness;   // bottom edge of the ceiling before any drop
        public const float DangerLineY = CeilingBaseY - (MaxRows - 1) * RowHeight - BubbleDiameter;

        public static readonly Vector2 CannonPivot = new Vector2(0f, -2.84f);

        public const float ShotSpeed = 15f;
        public const float MinAimAngle = 12f;
        public const float MaxAimAngle = 168f;
        public const float KeyboardAimSpeed = 110f;   // degrees per second

        public const int MinMatch = 3;

        // Sprite sorting orders, back to front.
        public const int OrderBackdrop = -100;
        public const int OrderBoardArt = -50;
        public const int OrderBoardFog = -45;
        public const int OrderCeiling = 5;
        public const int OrderBubble = 10;
        public const int OrderFalling = 12;
        public const int OrderFlying = 14;
        public const int OrderCannon = 16;
        public const int OrderFx = 20;
        public const int OrderVignette = 100;
    }
}
