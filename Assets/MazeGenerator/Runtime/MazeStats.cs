using System.Collections.Generic;

namespace MazeGenerator
{
    public struct MazeStats
    {
        public int SolutionLength;
        public Dictionary<TileShapeType, int> ShapeCounts;
    }

    public static class MazeStatsCalculator
    {
        public static MazeStats Calculate(MazeGrid grid, MazeSettings settings)
        {
            var shapeCounts = new Dictionary<TileShapeType, int>();
            foreach (var shape in TileShape.All) shapeCounts[shape] = 0;

            int size = grid.Size;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int mask = grid.GetMask(x, y);
                    if (!TileShape.TryClassify(mask, settings, out var shape, out _))
                        shape = TileShapeType.Closed;

                    shapeCounts[shape]++;
                }
            }

            return new MazeStats
            {
                SolutionLength = ComputeSolutionLength(grid),
                ShapeCounts = shapeCounts
            };
        }

        private static int ComputeSolutionLength(MazeGrid grid)
        {
            var distances = MazeGraph.BreadthFirstDistances(grid, grid.StartX, grid.StartY);
            return distances.TryGetValue((grid.ExitX, grid.ExitY), out int distance) ? distance : -1;
        }
    }
}
