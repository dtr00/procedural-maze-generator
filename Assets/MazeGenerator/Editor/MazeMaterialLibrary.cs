using UnityEditor;
using UnityEngine;

namespace MazeGenerator.Editor
{
    /// <summary>
    /// Creates (once) and reloads the URP Lit materials the maze builder needs, saved as
    /// real assets so they survive being included in a saved prefab.
    /// </summary>
    internal static class MazeMaterialLibrary
    {
        private const string FolderPath = "Assets/MazeGenerator/Materials";

        public static MazeMaterialSet LoadOrCreate()
        {
            EnsureFolder();

            var materials = new MazeMaterialSet
            {
                Floor = LoadOrCreateMaterial("MazeFloor", new Color(0.55f, 0.55f, 0.55f)),
                Wall = LoadOrCreateMaterial("MazeWall", new Color(0.78f, 0.78f, 0.78f)),
                Start = LoadOrCreateMaterial("MazeStart", new Color(0.15f, 0.85f, 0.2f)),
                Exit = LoadOrCreateMaterial("MazeExit", new Color(0.9f, 0.15f, 0.15f))
            };

            AssetDatabase.SaveAssets();
            return materials;
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(FolderPath)) return;
            AssetDatabase.CreateFolder("Assets/MazeGenerator", "Materials");
        }

        private static Material LoadOrCreateMaterial(string name, Color color)
        {
            string path = $"{FolderPath}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
