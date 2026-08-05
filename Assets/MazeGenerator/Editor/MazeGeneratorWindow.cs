using UnityEditor;
using UnityEngine;

namespace MazeGenerator.Editor
{
    /// <summary>
    /// Tools > Maze Generator. Lets a developer pick a size, enable/disable tile types,
    /// and generate a fresh random maze into the open scene as plain GameObjects.
    /// </summary>
    public sealed class MazeGeneratorWindow : EditorWindow
    {
        [SerializeField] private MazeSettings settings = new MazeSettings();
        [SerializeField] private GameObject lastRoot;

        private MazeGenerationResult lastResult;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Maze Generator")]
        private static void Open()
        {
            var window = GetWindow<MazeGeneratorWindow>("Maze Generator");
            window.minSize = new Vector2(340, 480);
        }

        private void OnEnable()
        {
            var existing = FindObjectsByType<MazeInstance>(FindObjectsInactive.Include);
            if (existing.Length > 0) lastRoot = existing[0].gameObject;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawSizeAndSeedSection();
            EditorGUILayout.Space();
            DrawTileToggleSection();
            EditorGUILayout.Space();
            DrawDimensionSection();
            EditorGUILayout.Space();
            DrawActionButtons();
            EditorGUILayout.Space();
            DrawResultSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSizeAndSeedSection()
        {
            EditorGUILayout.LabelField("Maze", EditorStyles.boldLabel);
            settings.size = (MazeSize)EditorGUILayout.EnumPopup("Size", settings.size);

            bool wasLocked = settings.lockSeed;
            settings.lockSeed = EditorGUILayout.ToggleLeft("Lock seed (reproduce a specific maze)", settings.lockSeed);
            if (!wasLocked && settings.lockSeed && lastResult != null && lastResult.Success)
                settings.lockedSeed = lastResult.Seed;

            using (new EditorGUI.DisabledScope(!settings.lockSeed))
            {
                int displaySeed = settings.lockSeed ? settings.lockedSeed : (lastResult != null ? lastResult.Seed : 0);
                int edited = EditorGUILayout.IntField("Seed", displaySeed);
                if (settings.lockSeed) settings.lockedSeed = edited;
            }

            if (!settings.lockSeed)
                EditorGUILayout.HelpBox("Every Generate uses a brand new random seed. Turn on Lock seed to reproduce the maze shown below.", MessageType.None);
        }

        private void DrawTileToggleSection()
        {
            EditorGUILayout.LabelField("Enabled Tile Types", EditorStyles.boldLabel);
            settings.allowClosed = EditorGUILayout.ToggleLeft("Closed (walled-off filler, used only if a cell can't be connected)", settings.allowClosed);
            settings.allowDeadEnd = EditorGUILayout.ToggleLeft("Dead End (1 opening)", settings.allowDeadEnd);
            settings.allowStraight = EditorGUILayout.ToggleLeft("Straight (2 opposite openings)", settings.allowStraight);
            settings.allowCorner = EditorGUILayout.ToggleLeft("Corner (2 adjacent openings)", settings.allowCorner);
            settings.allowTJunction = EditorGUILayout.ToggleLeft("T-Junction (3 openings)", settings.allowTJunction);
            settings.allowCrossroads = EditorGUILayout.ToggleLeft("Crossroads (4 openings)", settings.allowCrossroads);

            bool anyOpeningShape = settings.allowDeadEnd || settings.allowStraight || settings.allowCorner ||
                                    settings.allowTJunction || settings.allowCrossroads;
            if (!anyOpeningShape)
                EditorGUILayout.HelpBox("At least one tile type besides Closed must be enabled so the maze can have a path.", MessageType.Warning);
        }

        private void DrawDimensionSection()
        {
            EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
            settings.tileSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Tile Size", settings.tileSize));
            settings.wallHeight = Mathf.Max(0.1f, EditorGUILayout.FloatField("Wall Height", settings.wallHeight));
            settings.wallThickness = Mathf.Max(0.02f, EditorGUILayout.FloatField("Wall Thickness", settings.wallThickness));
            settings.floorThickness = Mathf.Max(0.02f, EditorGUILayout.FloatField("Floor Thickness", settings.floorThickness));
            settings.showLabels = EditorGUILayout.ToggleLeft("Show START / EXIT text labels", settings.showLabels);
        }

        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate")) Generate();
                if (GUILayout.Button("Clear")) Clear();
            }

            using (new EditorGUI.DisabledScope(lastRoot == null))
            {
                if (GUILayout.Button("Save As Prefab...")) SaveAsPrefab();
            }
        }

        private void DrawResultSection()
        {
            if (lastResult == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);

            if (!lastResult.Success)
            {
                EditorGUILayout.HelpBox(lastResult.FailureReason, MessageType.Error);
                return;
            }

            var grid = lastResult.Grid;
            EditorGUILayout.LabelField("Seed used", lastResult.Seed.ToString());
            EditorGUILayout.LabelField("Start", $"({grid.StartX}, {grid.StartY})");
            EditorGUILayout.LabelField("Exit", $"({grid.ExitX}, {grid.ExitY})");

            DrawStats(grid);
            DrawPreview(grid);
        }

        private void DrawStats(MazeGrid grid)
        {
            var stats = MazeStatsCalculator.Calculate(grid, settings);
            EditorGUILayout.LabelField("Solution length", stats.SolutionLength.ToString());

            foreach (var shape in TileShape.All)
                EditorGUILayout.LabelField(shape.ToString(), stats.ShapeCounts[shape].ToString());
        }

        private void DrawPreview(MazeGrid grid)
        {
            int size = grid.Size;
            float cellPixels = Mathf.Min(20f, 260f / size);

            Rect area = GUILayoutUtility.GetRect(cellPixels * size, cellPixels * size);
            EditorGUI.DrawRect(area, new Color(0.1f, 0.1f, 0.1f));

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int row = size - 1 - y; // north (higher grid y) drawn near the top of the preview
                    Rect cellRect = new Rect(area.x + x * cellPixels, area.y + row * cellPixels, cellPixels, cellPixels);

                    bool isStart = x == grid.StartX && y == grid.StartY;
                    bool isExit = x == grid.ExitX && y == grid.ExitY;
                    Color fillColor = isStart ? new Color(0.2f, 0.8f, 0.25f) :
                                       isExit ? new Color(0.85f, 0.2f, 0.2f) :
                                       new Color(0.25f, 0.25f, 0.25f);

                    EditorGUI.DrawRect(new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.width - 2, cellRect.height - 2), fillColor);

                    int mask = grid.GetMask(x, y);
                    float wallPixels = Mathf.Max(1f, cellPixels * 0.12f);

                    if ((mask & Direction.North) == 0)
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, wallPixels), Color.white);
                    if ((mask & Direction.South) == 0)
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - wallPixels, cellRect.width, wallPixels), Color.white);
                    if ((mask & Direction.East) == 0)
                        EditorGUI.DrawRect(new Rect(cellRect.xMax - wallPixels, cellRect.y, wallPixels, cellRect.height), Color.white);
                    if ((mask & Direction.West) == 0)
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, wallPixels, cellRect.height), Color.white);
                }
            }
        }

        private void Generate()
        {
            lastResult = MazeGeneratorEngine.Generate(settings);

            if (!lastResult.Success)
            {
                Debug.LogWarning($"Maze Generator: {lastResult.FailureReason}");
                Repaint();
                return;
            }

            var materials = MazeMaterialLibrary.LoadOrCreate();
            var root = MazeBuilder.Build(lastResult.Grid, settings, lastResult.Seed, materials);
            Undo.RegisterCreatedObjectUndo(root, "Generate Maze");

            ReplaceRoot(root);
            Repaint();
        }

        private void ReplaceRoot(GameObject newRoot)
        {
            var existing = FindObjectsByType<MazeInstance>(FindObjectsInactive.Include);
            foreach (var instance in existing)
            {
                if (instance.gameObject == newRoot) continue;
                Undo.DestroyObjectImmediate(instance.gameObject);
            }

            lastRoot = newRoot;
            Selection.activeGameObject = newRoot;
        }

        private void Clear()
        {
            var existing = FindObjectsByType<MazeInstance>(FindObjectsInactive.Include);
            foreach (var instance in existing)
                Undo.DestroyObjectImmediate(instance.gameObject);

            lastRoot = null;
            lastResult = null;
            Repaint();
        }

        private void SaveAsPrefab()
        {
            if (lastRoot == null) return;

            string defaultName = lastRoot.name.Replace(" ", "");
            string path = EditorUtility.SaveFilePanelInProject("Save Maze Prefab", defaultName, "prefab",
                "Choose a location for the maze prefab.");
            if (string.IsNullOrEmpty(path)) return;

            PrefabUtility.SaveAsPrefabAssetAndConnect(lastRoot, path, InteractionMode.UserAction);
        }
    }
}
