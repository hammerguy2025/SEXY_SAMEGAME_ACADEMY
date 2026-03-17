using System;
using System.Collections.Generic;
using UnityEngine;

namespace SameGame.Runtime
{
    public readonly struct SameGameMoveResult
    {
        public static readonly SameGameMoveResult None = new SameGameMoveResult(0, 0);

        public SameGameMoveResult(int removedCount, int scoreDelta)
        {
            RemovedCount = removedCount;
            ScoreDelta = scoreDelta;
        }

        public int RemovedCount { get; }

        public int ScoreDelta { get; }

        public bool RemovedAny => RemovedCount >= 2;
    }

    public sealed class SameGameBoard
    {
        public const int ClearBoardBonus = 300;

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private readonly int[] _cells;

        private SameGameBoard(BoardConfig config, int[] cells)
        {
            Config = config.Clone();
            _cells = cells;
        }

        public BoardConfig Config { get; }

        public int Width => Config.width;

        public int Height => Config.height;

        public int GetCell(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return -1;
            }

            return _cells[ToIndex(x, y)];
        }

        public static SameGameBoard CreateRandom(BoardConfig config, int seed)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.width <= 0 || config.height <= 0 || config.colorCount <= 1)
            {
                throw new ArgumentException("BoardConfig must define a positive board and at least two colors.");
            }

            var boardSize = config.width * config.height;
            var attempt = 0;

            while (true)
            {
                var random = new System.Random(unchecked(seed + (attempt * 977)));
                var cells = new int[boardSize];

                for (var i = 0; i < cells.Length; i++)
                {
                    cells[i] = random.Next(0, config.colorCount);
                }

                if (attempt >= 127 && boardSize >= 2)
                {
                    ForcePlayableGroup(cells, config, random);
                }

                var board = new SameGameBoard(config, cells);
                if (board.HasMoves() || attempt >= 127)
                {
                    return board;
                }

                attempt++;
            }
        }

        public static SameGameBoard FromCells(BoardConfig config, int[] cells)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Length != config.width * config.height)
            {
                throw new ArgumentException("Cell array length does not match board dimensions.");
            }

            var copy = new int[cells.Length];
            Array.Copy(cells, copy, cells.Length);
            return new SameGameBoard(config, copy);
        }

        public List<Vector2Int> GetGroupCells(int x, int y)
        {
            var result = new List<Vector2Int>();
            if (!IsInBounds(x, y))
            {
                return result;
            }

            var color = GetCell(x, y);
            if (color < 0)
            {
                return result;
            }

            var visited = new bool[_cells.Length];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(x, y));
            visited[ToIndex(x, y)] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                for (var i = 0; i < NeighborOffsets.Length; i++)
                {
                    var neighbor = current + NeighborOffsets[i];
                    if (!IsInBounds(neighbor.x, neighbor.y))
                    {
                        continue;
                    }

                    var neighborIndex = ToIndex(neighbor.x, neighbor.y);
                    if (visited[neighborIndex] || _cells[neighborIndex] != color)
                    {
                        continue;
                    }

                    visited[neighborIndex] = true;
                    queue.Enqueue(neighbor);
                }
            }

            return result;
        }

        public List<List<Vector2Int>> GetRemovableGroups()
        {
            var groups = new List<List<Vector2Int>>();
            var visited = new bool[_cells.Length];

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var index = ToIndex(x, y);
                    if (visited[index] || _cells[index] < 0)
                    {
                        continue;
                    }

                    var group = FloodFill(x, y, visited);
                    if (group.Count >= 2)
                    {
                        groups.Add(group);
                    }
                }
            }

            return groups;
        }

        public bool HasMoves()
        {
            var visited = new bool[_cells.Length];

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var index = ToIndex(x, y);
                    if (visited[index] || _cells[index] < 0)
                    {
                        continue;
                    }

                    var group = FloodFill(x, y, visited);
                    if (group.Count >= 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public SameGameMoveResult TryRemoveGroup(int x, int y)
        {
            var group = GetGroupCells(x, y);
            if (group.Count < 2)
            {
                return SameGameMoveResult.None;
            }

            for (var i = 0; i < group.Count; i++)
            {
                var cell = group[i];
                _cells[ToIndex(cell.x, cell.y)] = -1;
            }

            CollapseDown();
            CollapseColumnsLeft();

            return new SameGameMoveResult(group.Count, CalculateScore(group.Count));
        }

        public int CountOccupiedCells()
        {
            var count = 0;
            for (var i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CalculateScore(int removedCount)
        {
            if (removedCount < 2)
            {
                return 0;
            }

            var offset = removedCount - 2;
            return offset * offset;
        }

        public static int CalculateTurnScore(int removedCount, bool clearedBoard)
        {
            return CalculateScore(removedCount) + (clearedBoard ? ClearBoardBonus : 0);
        }

        private static void ForcePlayableGroup(int[] cells, BoardConfig config, System.Random random)
        {
            var pivotIndex = random.Next(0, cells.Length - 1);
            var pivotX = pivotIndex % config.width;
            var pivotY = pivotIndex / config.width;

            var adjacentIndex = pivotX < config.width - 1
                ? pivotIndex + 1
                : pivotIndex - 1;

            if (adjacentIndex < 0 || adjacentIndex >= cells.Length)
            {
                adjacentIndex = pivotY < config.height - 1
                    ? pivotIndex + config.width
                    : pivotIndex - config.width;
            }

            if (adjacentIndex < 0 || adjacentIndex >= cells.Length)
            {
                adjacentIndex = (pivotIndex + 1) % cells.Length;
            }

            cells[adjacentIndex] = cells[pivotIndex];
        }

        private List<Vector2Int> FloodFill(int x, int y, bool[] visited)
        {
            var result = new List<Vector2Int>();
            var color = GetCell(x, y);
            if (color < 0)
            {
                return result;
            }

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(x, y));
            visited[ToIndex(x, y)] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                for (var i = 0; i < NeighborOffsets.Length; i++)
                {
                    var neighbor = current + NeighborOffsets[i];
                    if (!IsInBounds(neighbor.x, neighbor.y))
                    {
                        continue;
                    }

                    var neighborIndex = ToIndex(neighbor.x, neighbor.y);
                    if (visited[neighborIndex] || _cells[neighborIndex] != color)
                    {
                        continue;
                    }

                    visited[neighborIndex] = true;
                    queue.Enqueue(neighbor);
                }
            }

            return result;
        }

        private void CollapseDown()
        {
            for (var x = 0; x < Width; x++)
            {
                var writeY = 0;
                for (var y = 0; y < Height; y++)
                {
                    var value = GetCell(x, y);
                    if (value < 0)
                    {
                        continue;
                    }

                    if (writeY != y)
                    {
                        _cells[ToIndex(x, writeY)] = value;
                        _cells[ToIndex(x, y)] = -1;
                    }

                    writeY++;
                }

                for (var y = writeY; y < Height; y++)
                {
                    _cells[ToIndex(x, y)] = -1;
                }
            }
        }

        private void CollapseColumnsLeft()
        {
            var writeX = 0;
            for (var x = 0; x < Width; x++)
            {
                if (IsColumnEmpty(x))
                {
                    continue;
                }

                if (writeX != x)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        _cells[ToIndex(writeX, y)] = _cells[ToIndex(x, y)];
                        _cells[ToIndex(x, y)] = -1;
                    }
                }

                writeX++;
            }

            for (var x = writeX; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    _cells[ToIndex(x, y)] = -1;
                }
            }
        }

        private bool IsColumnEmpty(int x)
        {
            for (var y = 0; y < Height; y++)
            {
                if (GetCell(x, y) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        private int ToIndex(int x, int y)
        {
            return y * Width + x;
        }
    }
}
