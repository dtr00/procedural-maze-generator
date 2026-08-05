namespace MazeGenerator
{
    /// <summary>The outcome of a generation attempt: either a usable grid, or a reason it could not be built.</summary>
    public sealed class MazeGenerationResult
    {
        public MazeGrid Grid { get; }
        public int Seed { get; }
        public bool Success { get; }
        public string FailureReason { get; }

        private MazeGenerationResult(MazeGrid grid, int seed, bool success, string failureReason)
        {
            Grid = grid;
            Seed = seed;
            Success = success;
            FailureReason = failureReason;
        }

        public static MazeGenerationResult Ok(MazeGrid grid, int seed) => new MazeGenerationResult(grid, seed, true, null);

        public static MazeGenerationResult Fail(int seed, string reason) => new MazeGenerationResult(null, seed, false, reason);
    }
}
