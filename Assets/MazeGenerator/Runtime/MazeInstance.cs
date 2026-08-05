using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Marker component placed on a generated maze's root object so the tool can find
    /// its own output later, and so the settings and seed used are visible on the object.
    /// </summary>
    public sealed class MazeInstance : MonoBehaviour
    {
        [SerializeField] private MazeSettings settings;
        [SerializeField] private int seed;

        public MazeSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public int Seed
        {
            get => seed;
            set => seed = value;
        }
    }
}
