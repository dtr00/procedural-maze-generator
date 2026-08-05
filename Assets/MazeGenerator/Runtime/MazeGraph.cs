using System.Collections.Generic;

namespace MazeGenerator
{
    /// <summary>
    /// Graph queries shared by generation (finding a valid border start/exit pair)
    /// and reporting (measuring the solution length).
    /// </summary>
    public static class MazeGraph
    {
        public static Dictionary<(int x, int y), int> BreadthFirstDistances(MazeGrid grid, int startX, int startY)
        {
            var distances = new Dictionary<(int x, int y), int> { [(startX, startY)] = 0 };
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                int distance = distances[(x, y)];
                int mask = grid.GetMask(x, y);

                foreach (int direction in Direction.All)
                {
                    if ((mask & direction) == 0) continue;

                    int nx = x + Direction.OffsetX(direction);
                    int ny = y + Direction.OffsetY(direction);
                    var neighbor = (nx, ny);
                    if (distances.ContainsKey(neighbor)) continue;

                    distances[neighbor] = distance + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return distances;
        }
    }
}
