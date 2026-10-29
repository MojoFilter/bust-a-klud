using System;
using System.Collections.Generic;
using UnityEngine;

namespace BustAKlud
{
    public readonly struct LevelSpec
    {
        public readonly int Number;
        public readonly string Name;
        public readonly int ColorCount;
        public readonly int Rows;
        public readonly int ShotLimit;   // shots (hit or miss) between ceiling drops

        public LevelSpec(int number, string name, int colorCount, int rows, int shotLimit)
        {
            Number = number;
            Name = name;
            ColorCount = colorCount;
            Rows = rows;
            ShotLimit = shotLimit;
        }
    }

    /// <summary>
    /// Builds the opening bubble layout for a level. Layouts are deterministic per level number: a silhouette
    /// (solid, pyramid, hourglass...) filled with colour clumps so every level has matches to find.
    /// </summary>
    public static class LevelGenerator
    {
        const int ShapeCount = 7;
        const float ClumpChance = 0.3f;

        static readonly string[] Names =
        {
            "Graveyard Shift", "Pumpkin Patch Panic", "The Cobweb Cellar", "Haunted Hallway",
            "Bat Belfry", "Skulls & Crossbuns", "Moonlit Mischief", "Ghoul Gallery",
            "Witching Hour", "Creepy Crawl", "Coffin Dodgers", "Midnight Mayhem"
        };

        static readonly Cell[] neighborBuffer = new Cell[6];

        public static LevelSpec GetSpec(int level)
        {
            level = Mathf.Max(1, level);
            int colors = level <= 2 ? 3 : level <= 5 ? 4 : 5;
            int rows = Mathf.Min(8, 4 + (level + 1) / 2);
            // Every shot counts now (not just misses), so the count starts roomy and tightens as levels go on.
            int shotLimit = Mathf.Max(4, 6 - (level - 1) / 3);
            return new LevelSpec(level, Names[(level - 1) % Names.Length], colors, rows, shotLimit);
        }

        public static void Fill(HexGrid grid, int level)
        {
            LevelSpec spec = GetSpec(level);
            var rng = new System.Random(level * 7919 + 17);

            grid.Clear();
            List<BubbleType> palette = PickPalette(rng, spec.ColorCount);
            int shape = (level - 1) % ShapeCount;

            for (int r = 0; r < spec.Rows; r++)
            {
                for (int c = 0; c < HexGrid.ColumnsInRow(r); c++)
                {
                    if (!InShape(shape, r, c, spec.Rows))
                        continue;
                    grid.Set(new Cell(r, c), PickColor(grid, rng, palette, r, c));
                }
            }

            // Shapes with gaps can strand bubbles; anything not hanging from the ceiling goes.
            var floating = new List<Cell>();
            grid.FindFloating(floating);
            foreach (Cell cell in floating)
                grid.Set(cell, BubbleType.None);
        }

        static List<BubbleType> PickPalette(System.Random rng, int count)
        {
            var all = new List<BubbleType>
            {
                BubbleType.Bat, BubbleType.Pumpkin, BubbleType.Skull, BubbleType.Ghost, BubbleType.Spider
            };
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            all.RemoveRange(count, all.Count - count);
            return all;
        }

        // Copy a neighbour that is already placed most of the time so colours form clumps.
        static BubbleType PickColor(HexGrid grid, System.Random rng, List<BubbleType> palette, int row, int col)
        {
            if (rng.NextDouble() < ClumpChance)
            {
                int count = grid.GetNeighbors(new Cell(row, col), neighborBuffer);
                var placed = new List<BubbleType>(3);
                for (int i = 0; i < count; i++)
                {
                    BubbleType type = grid.Get(neighborBuffer[i]);
                    if (type != BubbleType.None)
                        placed.Add(type);
                }

                if (placed.Count > 0)
                    return placed[rng.Next(placed.Count)];
            }

            return palette[rng.Next(palette.Count)];
        }

        static bool InShape(int shape, int row, int col, int rows)
        {
            int width = HexGrid.ColumnsInRow(row);
            float mid = (rows - 1) / 2f;

            switch (shape)
            {
                case 1: // pyramid, narrowing downwards
                {
                    int margin = row / 2;
                    return col >= margin && col < width - margin;
                }
                case 2: // inverted pyramid, widening downwards
                {
                    int margin = (rows - 1 - row) / 2;
                    return col >= margin && col < width - margin;
                }
                case 3: // hourglass
                {
                    int margin = Mathf.RoundToInt((mid - Mathf.Abs(row - mid)) * 0.8f);
                    return col >= margin && col < width - margin;
                }
                case 4: // lattice with holes
                    return row == 0 || (row + col) % 3 != 0;
                case 5: // frame with a core
                    return row == 0 || row == rows - 1 || col <= 1 || col >= width - 2 || (row == rows / 2 && col > 2 && col < width - 3);
                case 6: // diamond
                {
                    float halfWidth = (width - 1) / 2f;
                    float reach = (1f - Mathf.Abs(row - mid) / (mid + 1f)) * (halfWidth + 1f);
                    return Mathf.Abs(col - halfWidth) <= reach;
                }
                default: // solid block
                    return true;
            }
        }
    }
}
