using System;
using System.Collections.Generic;

namespace MazeGenerator
{
    /// <summary>
    /// Builds a perfect (single-path, no-loop) maze grid whose cells only ever
    /// resolve to an enabled tile shape, using a fresh random seed every call
    /// unless the caller has locked one in via <see cref="MazeSettings.lockSeed"/>.
    /// </summary>
    public static class MazeGeneratorEngine
    {
        private const int MaxAttempts = 25;

        public static MazeGenerationResult Generate(MazeSettings settings)
        {
            int baseSeed = settings.lockSeed ? settings.lockedSeed : Environment.TickCount ^ Guid.NewGuid().GetHashCode();

            if (!HasAnyOpeningShapeEnabled(settings))
            {
                return MazeGenerationResult.Fail(baseSeed,
                    "Enable at least one tile type with an opening (Dead End, Straight, Corner, T-Junction, or Crossroads) - a maze made only of Closed tiles cannot have a path.");
            }

            int size = (int)settings.size;
            var seedSequence = new Random(baseSeed);
            int lastAttemptSeed = baseSeed;

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                int attemptSeed = seedSequence.Next();
                lastAttemptSeed = attemptSeed;
                var rng = new Random(attemptSeed);

                var grid = new MazeGrid(size);
                Carve(grid, settings, rng);

                if (!Repair(grid, settings)) continue;
                if (!TryPickBorderEndpoints(grid, rng, out int startX, out int startY, out int exitX, out int exitY)) continue;

                grid.StartX = startX;
                grid.StartY = startY;
                grid.ExitX = exitX;
                grid.ExitY = exitY;

                return MazeGenerationResult.Ok(grid, attemptSeed);
            }

            return MazeGenerationResult.Fail(lastAttemptSeed,
                "Could not build a maze with the enabled tile types after multiple attempts. Try enabling more tile types (especially Corner and T-Junction).");
        }

        private static bool HasAnyOpeningShapeEnabled(MazeSettings settings)
        {
            foreach (var shape in TileShape.All)
            {
                if (shape == TileShapeType.Closed) continue;
                if (settings.IsShapeEnabled(shape)) return true;
            }

            return false;
        }

        /// <summary>
        /// Randomized backtracking carve: grows a spanning tree over the grid, only opening an
        /// edge when both cells it touches could still legally resolve to some enabled shape.
        /// </summary>
        private static void Carve(MazeGrid grid, MazeSettings settings, Random rng)
        {
            int size = grid.Size;
            var visited = new bool[size, size];
            var stack = new Stack<(int x, int y)>();

            int rootX = rng.Next(size);
            int rootY = rng.Next(size);
            visited[rootX, rootY] = true;
            stack.Push((rootX, rootY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Peek();
                bool carved = false;

                foreach (int direction in ShuffledDirections(rng))
                {
                    int nx = x + Direction.OffsetX(direction);
                    int ny = y + Direction.OffsetY(direction);
                    if (!grid.InBounds(nx, ny) || visited[nx, ny]) continue;

                    int currentMask = grid.GetMask(x, y) | direction;
                    int neighborMask = grid.GetMask(nx, ny) | Direction.Opposite(direction);

                    if (!TileShape.HasAllowedSuperset(currentMask, settings) ||
                        !TileShape.HasAllowedSuperset(neighborMask, settings))
                        continue;

                    grid.SetMask(x, y, currentMask);
                    grid.SetMask(nx, ny, neighborMask);
                    visited[nx, ny] = true;
                    stack.Push((nx, ny));
                    carved = true;
                    break;
                }

                if (!carved) stack.Pop();
            }
        }

        private static int[] ShuffledDirections(Random rng)
        {
            var directions = (int[])Direction.All.Clone();
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (directions[i], directions[j]) = (directions[j], directions[i]);
            }

            return directions;
        }

        /// <summary>
        /// Closes off openings, cell by cell, until every cell's mask exactly matches an enabled
        /// shape. Each fixup only ever removes openings, so the pass is guaranteed to terminate.
        /// Returns false if a cell cannot be resolved (it would need to become Closed, but Closed
        /// is disabled), signalling the caller to retry with a different seed.
        /// </summary>
        private static bool Repair(MazeGrid grid, MazeSettings settings)
        {
            int size = grid.Size;
            int maxIterations = size * size * 4;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                bool anyInvalid = false;

                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        int mask = grid.GetMask(x, y);
                        if (mask == 0) continue;
                        if (TileShape.TryClassify(mask, settings, out _, out _)) continue;

                        anyInvalid = true;
                        if (!ReduceCellToValidShape(grid, settings, x, y)) return false;
                    }
                }

                if (!anyInvalid) break;
            }

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int mask = grid.GetMask(x, y);
                    if (mask == 0 && !settings.allowClosed) return false;
                    if (!TileShape.TryClassify(mask, settings, out _, out _)) return false;
                }
            }

            return true;
        }

        private static bool ReduceCellToValidShape(MazeGrid grid, MazeSettings settings, int x, int y)
        {
            int mask = grid.GetMask(x, y);

            foreach (int bit in Direction.All)
            {
                if ((mask & bit) == 0) continue;

                mask &= ~bit;
                grid.SetMask(x, y, mask);
                CloseNeighborSide(grid, x, y, bit);

                if (mask == 0) break;
                if (TileShape.TryClassify(mask, settings, out _, out _)) return true;
            }

            return mask == 0 && settings.allowClosed;
        }

        private static void CloseNeighborSide(MazeGrid grid, int x, int y, int bit)
        {
            int nx = x + Direction.OffsetX(bit);
            int ny = y + Direction.OffsetY(bit);
            if (!grid.InBounds(nx, ny)) return;

            int neighborMask = grid.GetMask(nx, ny) & ~Direction.Opposite(bit);
            grid.SetMask(nx, ny, neighborMask);
        }

        /// <summary>
        /// Picks a random border cell as the start, then uses BFS to find the farthest border
        /// cell it can actually reach as the exit. Tries every border cell as a candidate start
        /// (in random order) until a reachable pair is found.
        /// </summary>
        private static bool TryPickBorderEndpoints(MazeGrid grid, Random rng, out int startX, out int startY, out int exitX, out int exitY)
        {
            int size = grid.Size;
            var borderCells = new List<(int x, int y)>();

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (grid.IsBorder(x, y) && grid.GetMask(x, y) != 0)
                        borderCells.Add((x, y));
                }
            }

            Shuffle(borderCells, rng);

            foreach (var candidateStart in borderCells)
            {
                var distances = MazeGraph.BreadthFirstDistances(grid, candidateStart.x, candidateStart.y);

                int bestDistance = -1;
                (int x, int y) bestCell = default;

                foreach (var cell in borderCells)
                {
                    if (cell == candidateStart) continue;
                    if (!distances.TryGetValue(cell, out int distance)) continue;
                    if (distance <= bestDistance) continue;

                    bestDistance = distance;
                    bestCell = cell;
                }

                if (bestDistance > 0)
                {
                    startX = candidateStart.x;
                    startY = candidateStart.y;
                    exitX = bestCell.x;
                    exitY = bestCell.y;
                    return true;
                }
            }

            startX = startY = exitX = exitY = 0;
            return false;
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
