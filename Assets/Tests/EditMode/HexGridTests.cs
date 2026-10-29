using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BustAKlud.Tests
{
    public class HexGridTests
    {
        HexGrid grid;

        [SetUp]
        public void SetUp()
        {
            grid = new HexGrid();
        }

        [Test]
        public void LongRowsHoldEightAndShortRowsSeven()
        {
            Assert.AreEqual(8, HexGrid.ColumnsInRow(0));
            Assert.AreEqual(7, HexGrid.ColumnsInRow(1));
            Assert.AreEqual(8, HexGrid.ColumnsInRow(2));
            Assert.IsTrue(HexGrid.IsValid(0, 7));
            Assert.IsFalse(HexGrid.IsValid(1, 7));
        }

        [Test]
        public void RowsFitInsideTheBoardWalls()
        {
            for (int r = 0; r < 4; r++)
            {
                Vector2 first = HexGrid.CellCenter(r, 0, 0f);
                Vector2 last = HexGrid.CellCenter(r, HexGrid.ColumnsInRow(r) - 1, 0f);
                Assert.GreaterOrEqual(first.x - GameConfig.BubbleRadius, GameConfig.BoardLeft - 0.0001f);
                Assert.LessOrEqual(last.x + GameConfig.BubbleRadius, GameConfig.BoardRight + 0.0001f);
            }
        }

        [Test]
        public void NeighborsAreSymmetric()
        {
            var a = new Cell[6];
            var b = new Cell[6];
            for (int r = 0; r < GameConfig.StorageRows; r++)
            {
                for (int c = 0; c < HexGrid.ColumnsInRow(r); c++)
                {
                    var cell = new Cell(r, c);
                    int countA = grid.GetNeighbors(cell, a);
                    for (int i = 0; i < countA; i++)
                    {
                        int countB = grid.GetNeighbors(a[i], b);
                        bool found = false;
                        for (int j = 0; j < countB; j++)
                            found |= b[j].Equals(cell);
                        Assert.IsTrue(found, $"{a[i]} lists no way back to {cell}");
                    }
                }
            }
        }

        [Test]
        public void EveryNeighborSitsOneBubbleDiameterAway()
        {
            var buffer = new Cell[6];
            for (int r = 0; r < GameConfig.StorageRows; r++)
            {
                for (int c = 0; c < HexGrid.ColumnsInRow(r); c++)
                {
                    var cell = new Cell(r, c);
                    int count = grid.GetNeighbors(cell, buffer);
                    for (int i = 0; i < count; i++)
                    {
                        float distance = Vector2.Distance(grid.CellCenter(cell), grid.CellCenter(buffer[i]));
                        Assert.AreEqual(GameConfig.BubbleDiameter, distance, 0.001f, $"{cell} -> {buffer[i]}");
                    }
                }
            }
        }

        [Test]
        public void InteriorCellsHaveSixNeighbors()
        {
            var buffer = new Cell[6];
            Assert.AreEqual(6, grid.GetNeighbors(new Cell(3, 3), buffer));
            Assert.AreEqual(6, grid.GetNeighbors(new Cell(4, 3), buffer));
            Assert.AreEqual(2, grid.GetNeighbors(new Cell(0, 0), buffer));   // right, and one below
            Assert.AreEqual(5, grid.GetNeighbors(new Cell(1, 0), buffer));   // short row, left edge: no left neighbour
        }

        [Test]
        public void ClusterFindsOnlyConnectedSameColour()
        {
            grid.Set(new Cell(0, 0), BubbleType.Bat);
            grid.Set(new Cell(0, 1), BubbleType.Bat);
            grid.Set(new Cell(1, 0), BubbleType.Bat);
            grid.Set(new Cell(0, 2), BubbleType.Skull);
            grid.Set(new Cell(0, 4), BubbleType.Bat);   // same colour but not touching

            var cluster = new List<Cell>();
            grid.FindCluster(new Cell(0, 0), cluster);

            Assert.AreEqual(3, cluster.Count);
            CollectionAssert.DoesNotContain(cluster, new Cell(0, 4));
        }

        [Test]
        public void FloatingBubblesAreThoseCutOffFromTheCeiling()
        {
            grid.Set(new Cell(0, 3), BubbleType.Bat);
            grid.Set(new Cell(1, 3), BubbleType.Skull);   // hangs from (0,3)
            grid.Set(new Cell(4, 1), BubbleType.Ghost);   // adrift
            grid.Set(new Cell(5, 1), BubbleType.Ghost);   // adrift

            var floating = new List<Cell>();
            grid.FindFloating(floating);

            Assert.AreEqual(2, floating.Count);
            CollectionAssert.Contains(floating, new Cell(4, 1));
            CollectionAssert.Contains(floating, new Cell(5, 1));
        }

        [Test]
        public void LandingCellPrefersTheCeilingRowThenBubbleContacts()
        {
            Assert.IsTrue(grid.TryFindLandingCell(new Vector2(0.1f, grid.CeilingY - GameConfig.BubbleRadius), out Cell top));
            Assert.AreEqual(0, top.Row);

            grid.Set(new Cell(0, 3), BubbleType.Bat);
            Vector2 below = grid.CellCenter(new Cell(0, 3)) + new Vector2(0.05f, -GameConfig.RowHeight);
            Assert.IsTrue(grid.TryFindLandingCell(below, out Cell landing));
            Assert.AreEqual(1, landing.Row);
        }

        [Test]
        public void CeilingDropsShiftCellsDownAndCanEndTheGame()
        {
            float before = grid.CellCenter(new Cell(0, 0)).y;
            grid.AdvanceCeiling();
            Assert.AreEqual(before - GameConfig.RowHeight, grid.CellCenter(new Cell(0, 0)).y, 0.0001f);

            grid.Set(new Cell(GameConfig.MaxRows - 2, 0), BubbleType.Bat);
            Assert.IsFalse(grid.ExceedsLimit());
            grid.AdvanceCeiling();
            Assert.IsTrue(grid.ExceedsLimit());
        }
    }
}
