using System;

namespace MazeGenerator
{
    /// <summary>
    /// Bitmask directions used to describe which sides of a maze cell are open.
    /// Grid X maps to world X (east/west), grid Y maps to world Z (north/south).
    /// </summary>
    public static class Direction
    {
        public const int North = 1 << 0;
        public const int East = 1 << 1;
        public const int South = 1 << 2;
        public const int West = 1 << 3;

        public static readonly int[] All = { North, East, South, West };

        public static int Opposite(int direction)
        {
            switch (direction)
            {
                case North: return South;
                case East: return West;
                case South: return North;
                case West: return East;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static int OffsetX(int direction)
        {
            switch (direction)
            {
                case North: return 0;
                case East: return 1;
                case South: return 0;
                case West: return -1;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static int OffsetY(int direction)
        {
            switch (direction)
            {
                case North: return 1;
                case East: return 0;
                case South: return -1;
                case West: return 0;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        /// <summary>Rotates an open-side mask clockwise (as seen from above) by the given number of 90 degree steps.</summary>
        public static int RotateClockwise(int mask, int steps)
        {
            steps = ((steps % 4) + 4) % 4;
            for (int i = 0; i < steps; i++)
            {
                int rotated = 0;
                if ((mask & North) != 0) rotated |= East;
                if ((mask & East) != 0) rotated |= South;
                if ((mask & South) != 0) rotated |= West;
                if ((mask & West) != 0) rotated |= North;
                mask = rotated;
            }

            return mask;
        }

        public static float YRotationDegrees(int rotationSteps) => 90f * (((rotationSteps % 4) + 4) % 4);
    }
}
