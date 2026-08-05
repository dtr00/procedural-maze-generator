using System;

namespace MazeGenerator
{
    /// <summary>
    /// The six tile shapes a cell can resolve to, classified purely by how many
    /// of its four sides are open. Rotation turns each canonical shape into the
    /// matching mask for any of the four facings.
    /// </summary>
    public enum TileShapeType
    {
        Closed,
        DeadEnd,
        Straight,
        Corner,
        TJunction,
        Crossroads
    }

    public static class TileShape
    {
        public static readonly TileShapeType[] All =
        {
            TileShapeType.Closed,
            TileShapeType.DeadEnd,
            TileShapeType.Straight,
            TileShapeType.Corner,
            TileShapeType.TJunction,
            TileShapeType.Crossroads
        };

        /// <summary>The open-side mask for a shape in its unrotated (facing north) orientation.</summary>
        public static int CanonicalMask(TileShapeType shape)
        {
            switch (shape)
            {
                case TileShapeType.Closed:
                    return 0;
                case TileShapeType.DeadEnd:
                    return Direction.North;
                case TileShapeType.Straight:
                    return Direction.North | Direction.South;
                case TileShapeType.Corner:
                    return Direction.North | Direction.East;
                case TileShapeType.TJunction:
                    return Direction.North | Direction.East | Direction.South;
                case TileShapeType.Crossroads:
                    return Direction.North | Direction.East | Direction.South | Direction.West;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        /// <summary>
        /// Finds an enabled shape and rotation whose mask exactly equals the given open-side mask.
        /// </summary>
        public static bool TryClassify(int mask, MazeSettings settings, out TileShapeType shape, out int rotationSteps)
        {
            foreach (var candidate in All)
            {
                if (!settings.IsShapeEnabled(candidate)) continue;

                int canonical = CanonicalMask(candidate);
                for (int steps = 0; steps < 4; steps++)
                {
                    if (Direction.RotateClockwise(canonical, steps) == mask)
                    {
                        shape = candidate;
                        rotationSteps = steps;
                        return true;
                    }
                }
            }

            shape = TileShapeType.Closed;
            rotationSteps = 0;
            return false;
        }

        /// <summary>
        /// True if some enabled shape, at some rotation, has at least the given open sides -
        /// meaning the mask could still legally grow into a valid tile later.
        /// </summary>
        public static bool HasAllowedSuperset(int mask, MazeSettings settings)
        {
            foreach (var candidate in All)
            {
                if (!settings.IsShapeEnabled(candidate)) continue;

                int canonical = CanonicalMask(candidate);
                for (int steps = 0; steps < 4; steps++)
                {
                    int rotated = Direction.RotateClockwise(canonical, steps);
                    if ((rotated & mask) == mask) return true;
                }
            }

            return false;
        }
    }
}
