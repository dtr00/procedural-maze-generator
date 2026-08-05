using System;
using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Turns a generated <see cref="MazeGrid"/> into real scene geometry: one primitive-built
    /// cell per grid position, each built in its shape's canonical orientation and then rotated
    /// into place, so every tile shares identical width, length, and height.
    /// </summary>
    public static class MazeBuilder
    {
        public const string RootName = "Procedural Maze";

        public static GameObject Build(MazeGrid grid, MazeSettings settings, int seed, MazeMaterialSet materials)
        {
            var root = new GameObject(RootName);
            var instance = root.AddComponent<MazeInstance>();
            instance.Settings = settings.Clone();
            instance.Seed = seed;

            int size = grid.Size;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    BuildCell(root.transform, grid, settings, materials, x, y);
                }
            }

            BuildPerimeter(root.transform, grid, settings, materials);
            return root;
        }

        private static void BuildCell(Transform root, MazeGrid grid, MazeSettings settings, MazeMaterialSet materials, int x, int y)
        {
            int mask = grid.GetMask(x, y);
            if (!TileShape.TryClassify(mask, settings, out var shape, out int rotationSteps))
            {
                shape = TileShapeType.Closed;
                rotationSteps = 0;
            }

            bool isStart = x == grid.StartX && y == grid.StartY;
            bool isExit = x == grid.ExitX && y == grid.ExitY;

            var cell = new GameObject($"Cell_{x}_{y}_{shape}");
            cell.transform.SetParent(root, false);
            cell.transform.localPosition = new Vector3(x * settings.tileSize, 0f, y * settings.tileSize);
            cell.transform.localRotation = Quaternion.Euler(0f, Direction.YRotationDegrees(rotationSteps), 0f);

            BuildFloor(cell.transform, settings, materials, isStart, isExit);
            BuildWalls(cell.transform, shape, settings, materials);

            if ((isStart || isExit) && settings.showLabels)
                BuildLabel(cell.transform, isStart ? "START" : "EXIT", settings, rotationSteps);
        }

        private static void BuildFloor(Transform cell, MazeSettings settings, MazeMaterialSet materials, bool isStart, bool isExit)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(cell, false);
            floor.transform.localPosition = new Vector3(0f, -settings.floorThickness * 0.5f, 0f);
            floor.transform.localScale = new Vector3(settings.tileSize, settings.floorThickness, settings.tileSize);

            var material = isStart ? materials.Start : isExit ? materials.Exit : materials.Floor;
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildWalls(Transform cell, TileShapeType shape, MazeSettings settings, MazeMaterialSet materials)
        {
            int canonicalMask = TileShape.CanonicalMask(shape);

            foreach (int direction in Direction.All)
            {
                if ((canonicalMask & direction) != 0) continue;

                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Wall_" + DirectionName(direction);
                wall.transform.SetParent(cell, false);
                PositionWall(wall.transform, direction, canonicalMask, settings);
                wall.GetComponent<Renderer>().sharedMaterial = materials.Wall;
            }

            BuildCornerPost(cell, canonicalMask, Direction.North, Direction.East, settings, materials);
            BuildCornerPost(cell, canonicalMask, Direction.North, Direction.West, settings, materials);
            BuildCornerPost(cell, canonicalMask, Direction.South, Direction.East, settings, materials);
            BuildCornerPost(cell, canonicalMask, Direction.South, Direction.West, settings, materials);
        }

        /// <summary>
        /// Four cells share the square of space at each grid vertex, and each of them fills its own
        /// quarter of it. A wall slab already covers a corner whenever one of that corner's two sides
        /// is walled, so the only quarter left unaccounted for is the one where both sides are open -
        /// without this post a wall turning that vertex from the neighbouring cells steps diagonally
        /// around the missing quarter instead of turning squarely.
        /// </summary>
        private static void BuildCornerPost(Transform cell, int canonicalMask, int northSouth, int eastWest, MazeSettings settings, MazeMaterialSet materials)
        {
            if ((canonicalMask & northSouth) == 0 || (canonicalMask & eastWest) == 0) return;

            float slab = SlabThickness(settings);
            float edge = settings.tileSize * 0.5f - slab * 0.5f;
            float x = eastWest == Direction.East ? edge : -edge;
            float z = northSouth == Direction.North ? edge : -edge;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post_" + DirectionName(northSouth) + DirectionName(eastWest);
            post.transform.SetParent(cell, false);
            post.transform.localPosition = new Vector3(x, settings.wallHeight * 0.5f, z);
            post.transform.localScale = new Vector3(slab, settings.wallHeight, slab);
            post.GetComponent<Renderer>().sharedMaterial = materials.Wall;
        }

        /// <summary>
        /// A cell builds its own half of every wall it touches, pressed against the tile edge, so
        /// two neighbouring cells meet to form one wall of exactly the configured thickness. North
        /// and south slabs run the tile's full width and own the corners; east and west slabs stop
        /// short only at a corner a north/south slab is already filling. That way slabs always
        /// touch without overlapping (an overlap z-fights into a visible seam) and without
        /// leaving a gap where a wall runs on to the tile's edge.
        /// </summary>
        private static void PositionWall(Transform wall, int direction, int canonicalMask, MazeSettings settings)
        {
            float half = settings.tileSize * 0.5f;
            float slab = SlabThickness(settings);
            float wallY = settings.wallHeight * 0.5f;
            float edge = half - slab * 0.5f;

            float sideMax = (canonicalMask & Direction.North) == 0 ? half - slab : half;
            float sideMin = (canonicalMask & Direction.South) == 0 ? -half + slab : -half;
            float sideCenter = (sideMax + sideMin) * 0.5f;
            float sideLength = sideMax - sideMin;

            switch (direction)
            {
                case Direction.North:
                    wall.localPosition = new Vector3(0f, wallY, edge);
                    wall.localScale = new Vector3(settings.tileSize, settings.wallHeight, slab);
                    break;
                case Direction.South:
                    wall.localPosition = new Vector3(0f, wallY, -edge);
                    wall.localScale = new Vector3(settings.tileSize, settings.wallHeight, slab);
                    break;
                case Direction.East:
                    wall.localPosition = new Vector3(edge, wallY, sideCenter);
                    wall.localScale = new Vector3(slab, settings.wallHeight, sideLength);
                    break;
                case Direction.West:
                    wall.localPosition = new Vector3(-edge, wallY, sideCenter);
                    wall.localScale = new Vector3(slab, settings.wallHeight, sideLength);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        /// <summary>
        /// Closes the outer half of the walls around the edge of the grid, which no second cell
        /// exists to fill, so the maze's outer boundary is the same thickness as the walls inside it.
        /// </summary>
        private static void BuildPerimeter(Transform root, MazeGrid grid, MazeSettings settings, MazeMaterialSet materials)
        {
            float half = settings.tileSize * 0.5f;
            float slab = SlabThickness(settings);
            float wallY = settings.wallHeight * 0.5f;

            float min = -half;
            float max = (grid.Size - 1) * settings.tileSize + half;
            float center = (min + max) * 0.5f;
            float span = max - min;

            var perimeter = new GameObject("Perimeter");
            perimeter.transform.SetParent(root, false);

            // Same rule as the cell walls: north/south own the outer corners, east/west fit between.
            var acrossScale = new Vector3(span + slab * 2f, settings.wallHeight, slab);
            var alongScale = new Vector3(slab, settings.wallHeight, span);

            AddPerimeterSlab(perimeter.transform, "Wall_North", new Vector3(center, wallY, max + slab * 0.5f), acrossScale, materials);
            AddPerimeterSlab(perimeter.transform, "Wall_South", new Vector3(center, wallY, min - slab * 0.5f), acrossScale, materials);
            AddPerimeterSlab(perimeter.transform, "Wall_East", new Vector3(max + slab * 0.5f, wallY, center), alongScale, materials);
            AddPerimeterSlab(perimeter.transform, "Wall_West", new Vector3(min - slab * 0.5f, wallY, center), alongScale, materials);
        }

        private static void AddPerimeterSlab(Transform parent, string name, Vector3 position, Vector3 scale, MazeMaterialSet materials)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = materials.Wall;
        }

        /// <summary>
        /// Half the configured wall thickness, since two neighbouring cells each build half of the
        /// wall between them. Capped so a wall can never eat more than half the corridor beside it.
        /// </summary>
        private static float SlabThickness(MazeSettings settings)
        {
            return Mathf.Min(settings.wallThickness * 0.5f, settings.tileSize * 0.25f);
        }

        private static void BuildLabel(Transform cell, string text, MazeSettings settings, int rotationSteps)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(cell, false);
            label.transform.localPosition = new Vector3(0f, settings.wallHeight * 0.5f, 0f);
            // Counter-rotate against the cell's own rotation so the text always reads upright from above.
            label.transform.localRotation = Quaternion.Euler(90f, -Direction.YRotationDegrees(rotationSteps), 0f);

            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.characterSize = settings.tileSize * 0.08f;
            textMesh.fontSize = 48;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
        }

        private static string DirectionName(int direction)
        {
            switch (direction)
            {
                case Direction.North: return "North";
                case Direction.East: return "East";
                case Direction.South: return "South";
                case Direction.West: return "West";
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
