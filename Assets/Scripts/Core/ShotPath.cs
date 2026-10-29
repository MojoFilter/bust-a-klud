using System.Collections.Generic;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>A precomputed bubble trajectory: a polyline with wall bounces, ending where the bubble sticks.</summary>
    public class ShotPath
    {
        public readonly List<Vector2> Points = new List<Vector2>(16);
        readonly List<float> cumulative = new List<float>(16);

        public Cell LandingCell { get; set; }
        public Vector2 End => Points.Count > 0 ? Points[Points.Count - 1] : Vector2.zero;
        public float Length => cumulative.Count > 0 ? cumulative[cumulative.Count - 1] : 0f;
        public int BounceCount => Mathf.Max(0, Points.Count - 2);

        public void Clear()
        {
            Points.Clear();
            cumulative.Clear();
        }

        public void AddPoint(Vector2 point)
        {
            float previous = cumulative.Count > 0 ? cumulative[cumulative.Count - 1] : 0f;
            if (Points.Count > 0)
                previous += Vector2.Distance(Points[Points.Count - 1], point);
            Points.Add(point);
            cumulative.Add(previous);
        }

        /// <summary>Distance along the path at which the point with the given index sits.</summary>
        public float DistanceAtPoint(int index) => cumulative[index];

        public Vector2 PointAt(float distance)
        {
            if (Points.Count == 0)
                return Vector2.zero;
            if (distance <= 0f)
                return Points[0];
            if (distance >= Length)
                return End;

            int i = 1;
            while (i < cumulative.Count - 1 && cumulative[i] < distance)
                i++;

            float segment = cumulative[i] - cumulative[i - 1];
            float t = segment > 0f ? (distance - cumulative[i - 1]) / segment : 0f;
            return Vector2.Lerp(Points[i - 1], Points[i], t);
        }
    }

    /// <summary>
    /// Steps a bubble through the field exactly as the real shot will fly, so the aim guide never lies.
    /// </summary>
    public static class ShotPathSimulator
    {
        const float Step = 0.05f;
        const float StickDistance = GameConfig.BubbleDiameter * 0.9f;
        const int MaxSteps = 3000;

        static readonly List<Vector2> bubbleCenters = new List<Vector2>(128);

        public static void Simulate(HexGrid grid, Vector2 start, Vector2 direction, ShotPath path)
        {
            path.Clear();
            grid.CollectBubbleCenters(bubbleCenters);

            Vector2 dir = direction.normalized;
            Vector2 pos = start;
            path.AddPoint(pos);

            float left = GameConfig.BoardLeft + GameConfig.BubbleRadius;
            float right = GameConfig.BoardRight - GameConfig.BubbleRadius;
            float top = grid.CeilingY - GameConfig.BubbleRadius;
            float stickSqr = StickDistance * StickDistance;

            for (int i = 0; i < MaxSteps; i++)
            {
                Vector2 next = pos + dir * Step;

                if (next.x < left || next.x > right)
                {
                    float wall = next.x < left ? left : right;
                    float t = (wall - pos.x) / (next.x - pos.x);
                    Vector2 bounce = Vector2.Lerp(pos, next, t);
                    path.AddPoint(bounce);
                    dir.x = -dir.x;
                    next = bounce + dir * (Step * (1f - t));
                }

                pos = next;

                if (pos.y >= top)
                {
                    pos.y = top;
                    break;
                }

                if (TouchesBubble(pos, stickSqr))
                    break;
            }

            path.AddPoint(pos);

            if (!grid.TryFindLandingCell(pos, out Cell landing))
                landing = new Cell(0, 0);
            path.LandingCell = landing;
        }

        static bool TouchesBubble(Vector2 pos, float stickSqr)
        {
            for (int i = 0; i < bubbleCenters.Count; i++)
                if ((bubbleCenters[i] - pos).sqrMagnitude < stickSqr)
                    return true;
            return false;
        }
    }
}
