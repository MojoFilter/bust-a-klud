using System;
using System.Collections.Generic;
using UnityEngine;

namespace BustAKlud
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int Row;
        public readonly int Col;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool Equals(Cell other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => Row * 31 + Col;
        public override string ToString() => $"({Row},{Col})";
    }

    /// <summary>
    /// The logical bubble field: a hex grid whose even rows hold 8 bubbles and odd rows hold 7, offset by
    /// half a bubble. Rows are indexed from the ceiling; the ceiling "dropping" shifts every row's world
    /// position down by one row height without renumbering anything.
    /// </summary>
    public class HexGrid
    {
        static readonly int[] LongRowDiagonals = { -1, 0 };    // columns touched in the neighbouring rows
        static readonly int[] ShortRowDiagonals = { 0, 1 };

        readonly BubbleType[,] cells = new BubbleType[GameConfig.StorageRows, GameConfig.Columns];
        readonly bool[,] visited = new bool[GameConfig.StorageRows, GameConfig.Columns];
        readonly Queue<Cell> queue = new Queue<Cell>();
        readonly Cell[] neighborBuffer = new Cell[6];

        public int CeilingDrops { get; private set; }
        public float CeilingY => GameConfig.CeilingBaseY - CeilingDrops * GameConfig.RowHeight;

        public HexGrid()
        {
            Clear();
        }

        public void Clear()
        {
            for (int r = 0; r < GameConfig.StorageRows; r++)
                for (int c = 0; c < GameConfig.Columns; c++)
                    cells[r, c] = BubbleType.None;
            CeilingDrops = 0;
        }

        public static bool IsShortRow(int row) => (row & 1) == 1;
        public static int ColumnsInRow(int row) => IsShortRow(row) ? GameConfig.Columns - 1 : GameConfig.Columns;

        public static bool IsValid(int row, int col) =>
            row >= 0 && row < GameConfig.StorageRows && col >= 0 && col < ColumnsInRow(row);

        public BubbleType Get(Cell cell) => IsValid(cell.Row, cell.Col) ? cells[cell.Row, cell.Col] : BubbleType.None;
        public bool IsOccupied(Cell cell) => Get(cell) != BubbleType.None;

        public void Set(Cell cell, BubbleType type)
        {
            if (IsValid(cell.Row, cell.Col))
                cells[cell.Row, cell.Col] = type;
        }

        public void AdvanceCeiling()
        {
            CeilingDrops++;
        }

        public static Vector2 CellCenter(int row, int col, float ceilingDrops)
        {
            float x = GameConfig.BoardLeft + GameConfig.BubbleRadius + col * GameConfig.BubbleDiameter
                      + (IsShortRow(row) ? GameConfig.BubbleRadius : 0f);
            float y = GameConfig.CeilingBaseY - ceilingDrops * GameConfig.RowHeight
                      - GameConfig.BubbleRadius - row * GameConfig.RowHeight;
            return new Vector2(x, y);
        }

        public Vector2 CellCenter(Cell cell) => CellCenter(cell.Row, cell.Col, CeilingDrops);

        public int Count
        {
            get
            {
                int n = 0;
                for (int r = 0; r < GameConfig.StorageRows; r++)
                    for (int c = 0; c < ColumnsInRow(r); c++)
                        if (cells[r, c] != BubbleType.None)
                            n++;
                return n;
            }
        }

        public bool IsEmpty => Count == 0;

        /// <summary>Deepest occupied row, or -1 when the field is empty.</summary>
        public int LowestRow()
        {
            for (int r = GameConfig.StorageRows - 1; r >= 0; r--)
                for (int c = 0; c < ColumnsInRow(r); c++)
                    if (cells[r, c] != BubbleType.None)
                        return r;
            return -1;
        }

        /// <summary>True when any bubble has been pushed past the lose line.</summary>
        public bool ExceedsLimit()
        {
            int lowest = LowestRow();
            return lowest >= 0 && lowest + CeilingDrops >= GameConfig.MaxRows;
        }

        /// <summary>How many rows of headroom remain above the lose line (0 = one more row loses).</summary>
        public int RowsOfHeadroom()
        {
            int lowest = LowestRow();
            return GameConfig.MaxRows - 1 - (Math.Max(lowest, 0) + CeilingDrops);
        }

        public void TypesPresent(List<BubbleType> result)
        {
            result.Clear();
            for (int r = 0; r < GameConfig.StorageRows; r++)
                for (int c = 0; c < ColumnsInRow(r); c++)
                {
                    BubbleType type = cells[r, c];
                    if (type != BubbleType.None && !result.Contains(type))
                        result.Add(type);
                }
        }

        public void CollectBubbleCenters(List<Vector2> result)
        {
            result.Clear();
            for (int r = 0; r < GameConfig.StorageRows; r++)
                for (int c = 0; c < ColumnsInRow(r); c++)
                    if (cells[r, c] != BubbleType.None)
                        result.Add(CellCenter(r, c, CeilingDrops));
        }

        public int GetNeighbors(Cell cell, Cell[] buffer)
        {
            int n = 0;
            int r = cell.Row;
            int c = cell.Col;
            AddIfValid(buffer, ref n, r, c - 1);
            AddIfValid(buffer, ref n, r, c + 1);
            int[] diagonals = IsShortRow(r) ? ShortRowDiagonals : LongRowDiagonals;
            for (int dr = -1; dr <= 1; dr += 2)
                for (int i = 0; i < diagonals.Length; i++)
                    AddIfValid(buffer, ref n, r + dr, c + diagonals[i]);
            return n;
        }

        static void AddIfValid(Cell[] buffer, ref int n, int row, int col)
        {
            if (IsValid(row, col))
                buffer[n++] = new Cell(row, col);
        }

        /// <summary>Finds the same-type group containing <paramref name="start"/> (including it).</summary>
        public void FindCluster(Cell start, List<Cell> result)
        {
            result.Clear();
            BubbleType type = Get(start);
            if (type == BubbleType.None)
                return;

            ResetVisited();
            queue.Clear();
            queue.Enqueue(start);
            visited[start.Row, start.Col] = true;

            while (queue.Count > 0)
            {
                Cell current = queue.Dequeue();
                result.Add(current);
                int count = GetNeighbors(current, neighborBuffer);
                for (int i = 0; i < count; i++)
                {
                    Cell next = neighborBuffer[i];
                    if (visited[next.Row, next.Col] || cells[next.Row, next.Col] != type)
                        continue;
                    visited[next.Row, next.Col] = true;
                    queue.Enqueue(next);
                }
            }
        }

        /// <summary>Finds every bubble no longer connected to the ceiling through other bubbles.</summary>
        public void FindFloating(List<Cell> result)
        {
            result.Clear();
            ResetVisited();
            queue.Clear();

            for (int c = 0; c < ColumnsInRow(0); c++)
            {
                if (cells[0, c] == BubbleType.None)
                    continue;
                visited[0, c] = true;
                queue.Enqueue(new Cell(0, c));
            }

            while (queue.Count > 0)
            {
                Cell current = queue.Dequeue();
                int count = GetNeighbors(current, neighborBuffer);
                for (int i = 0; i < count; i++)
                {
                    Cell next = neighborBuffer[i];
                    if (visited[next.Row, next.Col] || cells[next.Row, next.Col] == BubbleType.None)
                        continue;
                    visited[next.Row, next.Col] = true;
                    queue.Enqueue(next);
                }
            }

            for (int r = 0; r < GameConfig.StorageRows; r++)
                for (int c = 0; c < ColumnsInRow(r); c++)
                    if (cells[r, c] != BubbleType.None && !visited[r, c])
                        result.Add(new Cell(r, c));
        }

        /// <summary>
        /// Picks the empty cell a bubble arriving at <paramref name="position"/> should snap into: the nearest
        /// empty cell that touches the ceiling or another bubble.
        /// </summary>
        public bool TryFindLandingCell(Vector2 position, out Cell landing)
        {
            landing = default;
            float best = float.MaxValue;
            bool found = false;

            for (int r = 0; r < GameConfig.StorageRows; r++)
            {
                for (int c = 0; c < ColumnsInRow(r); c++)
                {
                    if (cells[r, c] != BubbleType.None)
                        continue;

                    var candidate = new Cell(r, c);
                    if (r != 0 && !TouchesBubble(candidate))
                        continue;

                    float distance = (CellCenter(r, c, CeilingDrops) - position).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        landing = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        bool TouchesBubble(Cell cell)
        {
            int count = GetNeighbors(cell, neighborBuffer);
            for (int i = 0; i < count; i++)
                if (cells[neighborBuffer[i].Row, neighborBuffer[i].Col] != BubbleType.None)
                    return true;
            return false;
        }

        void ResetVisited()
        {
            Array.Clear(visited, 0, visited.Length);
        }
    }
}
