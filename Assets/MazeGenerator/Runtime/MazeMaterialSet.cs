using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// The four materials a built maze needs. Kept as plain data here so
    /// <see cref="MazeBuilder"/> stays free of editor-only asset creation code;
    /// the actual material assets are created by the editor window.
    /// </summary>
    public sealed class MazeMaterialSet
    {
        public Material Floor;
        public Material Wall;
        public Material Start;
        public Material Exit;
    }
}
