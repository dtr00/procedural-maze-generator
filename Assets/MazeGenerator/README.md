# Procedural Maze Generator Tool

A Unity Editor tool for generating grid-based mazes for level layouts. Open it from
**Tools > Maze Generator**.

## Installation

1. In Unity, **Assets > Import Package > Custom Package...** and choose
   `ProceduralMazeGenerator.unitypackage`.
2. Import everything. The tool installs to `Assets/MazeGenerator` and adds a
   **Tools > Maze Generator** menu item.

Built and tested in Unity 6 (6000.5). There are no package dependencies, and it works
in both URP and Built-in Render Pipeline projects - the materials are created against
whichever shader the project has available. Leave the folder at `Assets/MazeGenerator`,
since the generated materials are written to that path.

## How a maze is built

Every cell in the grid is defined entirely by which of its four sides are open
(North / East / South / West). That is a 4-bit mask, and all 16 possible masks are
covered by six tile shapes plus rotation:

| Shape | Openings | Example |
|---|---|---|
| Closed | 0 | fully walled filler block |
| Dead End | 1 | a single doorway |
| Straight | 2 (opposite) | a hallway passing through |
| Corner | 2 (adjacent) | a turn |
| T-Junction | 3 | a three-way branch |
| Crossroads | 4 | a four-way branch |

A tile's rotation is just how many 90-degree turns are applied to its canonical
(north-facing) shape, so every tile in the maze is built from the same primitive
geometry and simply rotated to fit the opening pattern the generator needs at that
cell - every tile has identical width, length, and height.

## Generating a maze

1. Choose a **Size**: 3x3, 7x7, or 9x9.
2. Enable or disable whichever tile shapes you want available. Disabling a shape is
   a hard constraint on generation, not a post-filter - the algorithm will never
   place a cell that needs a disabled shape.
3. Click **Generate**. Every click draws a brand new random seed, so consecutive
   generations differ from each other (beyond whatever the enabled shapes force).
   If you want to reproduce a maze you liked, turn on **Lock seed** right after
   generating it - the field fills in with the seed that produced the maze shown,
   and future Generate calls will reuse it exactly.
4. **Clear** removes the generated maze from the scene. **Save As Prefab...**
   writes the current maze out as a reusable prefab asset.

The start cell is marked green, the exit cell is marked red (colored floor, plus an
optional "START" / "EXIT" text label), and the exit is always chosen as the farthest
reachable border cell from the start, so the path is never trivial.

## Why "disable a tile" can make generation fail

Because a cell's shape is fixed by its actual number of connections, restricting the
allowed shapes restricts what the maze's structure can look like. For example,
disabling everything except Straight would only allow a maze that is a single
straight hallway with no turns - impossible on anything but a 1-wide grid. When the
enabled set can't produce a valid maze at the chosen size, the tool retries with
several different random seeds and, if none work, reports the failure in the window
instead of producing a broken result.

## Project layout

- `Runtime/` - plain C# maze model and generation logic (`Direction`, `TileShape`,
  `MazeSettings`, `MazeGrid`, `MazeGeneratorEngine`, `MazeBuilder`, `MazeInstance`,
  `MazeStats`). No editor dependencies, so this half of the tool could be reused for
  runtime generation later.
- `Editor/` - `MazeGeneratorWindow` (the Tools menu window) and `MazeMaterialLibrary`
  (creates the four URP materials used for floors, walls, start, and exit).
- `Materials/` - generated the first time you open the window; safe to reassign to
  your own materials afterward.

## Notes for developers

- Generated mazes live under a single `Procedural Maze` root object with a
  `MazeInstance` component recording the settings and seed used, so any maze can be
  inspected or reproduced later.
- Regenerating replaces the previous maze in the scene rather than stacking copies.
- Wall height, wall thickness, floor thickness, and tile size are all adjustable so
  the generated geometry can match your game's scale.
