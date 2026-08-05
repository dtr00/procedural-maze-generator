using System;

namespace MazeGenerator
{
    public enum MazeSize
    {
        Size3 = 3,
        Size7 = 7,
        Size9 = 9
    }

    /// <summary>
    /// Developer-facing options for a maze generation run. Every value here is
    /// safe to tweak from the editor window and re-generate against.
    /// </summary>
    [Serializable]
    public sealed class MazeSettings
    {
        public MazeSize size = MazeSize.Size7;

        public bool allowClosed = true;
        public bool allowDeadEnd = true;
        public bool allowStraight = true;
        public bool allowCorner = true;
        public bool allowTJunction = true;
        public bool allowCrossroads = true;

        public float tileSize = 4f;
        public float wallHeight = 3f;
        public float wallThickness = 0.4f;
        public float floorThickness = 0.2f;
        public bool showLabels = true;

        public bool lockSeed = false;
        public int lockedSeed = 0;

        public bool IsShapeEnabled(TileShapeType shape)
        {
            switch (shape)
            {
                case TileShapeType.Closed: return allowClosed;
                case TileShapeType.DeadEnd: return allowDeadEnd;
                case TileShapeType.Straight: return allowStraight;
                case TileShapeType.Corner: return allowCorner;
                case TileShapeType.TJunction: return allowTJunction;
                case TileShapeType.Crossroads: return allowCrossroads;
                default: throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        public MazeSettings Clone()
        {
            return new MazeSettings
            {
                size = size,
                allowClosed = allowClosed,
                allowDeadEnd = allowDeadEnd,
                allowStraight = allowStraight,
                allowCorner = allowCorner,
                allowTJunction = allowTJunction,
                allowCrossroads = allowCrossroads,
                tileSize = tileSize,
                wallHeight = wallHeight,
                wallThickness = wallThickness,
                floorThickness = floorThickness,
                showLabels = showLabels,
                lockSeed = lockSeed,
                lockedSeed = lockedSeed
            };
        }
    }
}
