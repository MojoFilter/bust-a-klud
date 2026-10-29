using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BustAKlud.Tests
{
    public class LevelAndShotTests
    {
        [Test]
        public void EveryLevelStartsWithSolidAnchoredBoard()
        {
            for (int level = 1; level <= 30; level++)
            {
                var grid = new HexGrid();
                LevelGenerator.Fill(grid, level);
                LevelSpec spec = LevelGenerator.GetSpec(level);

                Assert.Greater(grid.Count, 12, $"level {level} is nearly empty");
                Assert.LessOrEqual(grid.LowestRow(), spec.Rows - 1, $"level {level} exceeds its row budget");
                Assert.IsFalse(grid.ExceedsLimit(), $"level {level} starts lost");

                var floating = new List<Cell>();
                grid.FindFloating(floating);
                Assert.AreEqual(0, floating.Count, $"level {level} has bubbles not hanging from the ceiling");

                var types = new List<BubbleType>();
                grid.TypesPresent(types);
                Assert.LessOrEqual(types.Count, spec.ColorCount, $"level {level} uses too many colours");
            }
        }

        [Test]
        public void LevelsAreDeterministic()
        {
            var a = new HexGrid();
            var b = new HexGrid();
            LevelGenerator.Fill(a, 7);
            LevelGenerator.Fill(b, 7);

            for (int r = 0; r < GameConfig.StorageRows; r++)
                for (int c = 0; c < HexGrid.ColumnsInRow(r); c++)
                    Assert.AreEqual(a.Get(new Cell(r, c)), b.Get(new Cell(r, c)));
        }

        [Test]
        public void DifficultyRampsWithLevel()
        {
            LevelSpec early = LevelGenerator.GetSpec(1);
            LevelSpec late = LevelGenerator.GetSpec(12);
            Assert.GreaterOrEqual(late.ColorCount, early.ColorCount);
            Assert.GreaterOrEqual(late.Rows, early.Rows);
            Assert.LessOrEqual(late.ShotLimit, early.ShotLimit);
            Assert.GreaterOrEqual(late.ShotLimit, 4);
        }

        [Test]
        public void StraightShotOnEmptyBoardSticksToTheCeiling()
        {
            var grid = new HexGrid();
            var path = new ShotPath();
            ShotPathSimulator.Simulate(grid, new Vector2(0.1f, -2.5f), Vector2.up, path);

            Assert.AreEqual(0, path.LandingCell.Row);
            Assert.AreEqual(0, path.BounceCount);
            Assert.AreEqual(grid.CeilingY - GameConfig.BubbleRadius, path.End.y, 0.001f);
        }

        [Test]
        public void ShallowShotBouncesOffTheWallsAndStaysInside()
        {
            var grid = new HexGrid();
            var path = new ShotPath();
            Vector2 dir = new Vector2(Mathf.Cos(20f * Mathf.Deg2Rad), Mathf.Sin(20f * Mathf.Deg2Rad));
            ShotPathSimulator.Simulate(grid, new Vector2(0f, -2.5f), dir, path);

            Assert.Greater(path.BounceCount, 0);
            float left = GameConfig.BoardLeft + GameConfig.BubbleRadius - 0.001f;
            float right = GameConfig.BoardRight - GameConfig.BubbleRadius + 0.001f;
            foreach (Vector2 point in path.Points)
                Assert.That(point.x, Is.InRange(left, right));
        }

        [Test]
        public void ShotStopsAgainstABubbleAndLandsBelowIt()
        {
            var grid = new HexGrid();
            grid.Set(new Cell(0, 3), BubbleType.Bat);
            grid.Set(new Cell(0, 4), BubbleType.Bat);

            var path = new ShotPath();
            Vector2 target = grid.CellCenter(new Cell(0, 3));
            Vector2 start = new Vector2(target.x, -2.5f);
            ShotPathSimulator.Simulate(grid, start, Vector2.up, path);

            Assert.AreEqual(1, path.LandingCell.Row);
            Assert.Less(path.End.y, target.y);
            Assert.IsFalse(grid.IsOccupied(path.LandingCell));
        }

        [Test]
        public void PathPointAtWalksTheWholePolyline()
        {
            var path = new ShotPath();
            path.AddPoint(new Vector2(0f, 0f));
            path.AddPoint(new Vector2(0f, 2f));
            path.AddPoint(new Vector2(2f, 2f));

            Assert.AreEqual(4f, path.Length, 0.0001f);
            Assert.AreEqual(new Vector2(0f, 1f), path.PointAt(1f));
            Assert.AreEqual(new Vector2(1f, 2f), path.PointAt(3f));
            Assert.AreEqual(new Vector2(2f, 2f), path.PointAt(99f));
        }
    }
}
