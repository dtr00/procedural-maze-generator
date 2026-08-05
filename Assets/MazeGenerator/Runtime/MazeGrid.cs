namespace MazeGenerator
{
    /// <summary>
    /// A square grid of open-side masks plus the chosen start/exit cell coordinates.
    /// </summary>
    public sealed class MazeGrid
    {
        private readonly int[,] openMask;

        public int Size { get; }

        public int StartX { get; set; }
        public int StartY { get; set; }
        public int ExitX { get; set; }
        public int ExitY { get; set; }

        public MazeGrid(int size)
        {
            Size = size;
            openMask = new int[size, size];
        }

        public int GetMask(int x, int y) => openMask[x, y];

        public void SetMask(int x, int y, int mask) => openMask[x, y] = mask;

        public bool InBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

        public bool IsBorder(int x, int y) => x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
    }
}
