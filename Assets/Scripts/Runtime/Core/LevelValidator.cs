using System.Collections.Generic;
using System.Linq;

namespace BoxPushing.Core
{
    /// <summary>
    /// Static validation for <see cref="LevelData"/>: exactly one player, box count equals
    /// goal count, outermost ring fully enclosed by walls, no characters outside the XSB
    /// set, and BFS reachability (player connectivity + every box reachable).
    /// </summary>
    public static class LevelValidator
    {
        public static bool IsValid(LevelData level) => GetErrors(level).Count == 0;

        public static IReadOnlyList<string> GetErrors(LevelData level)
        {
            var errors = new List<string>();

            if (level == null)
            {
                errors.Add("Level is null.");
                return errors;
            }

            if (level.map == null || level.map.Length == 0)
            {
                errors.Add("Map is empty.");
                return errors;
            }

            int width = level.Width;
            if (level.map.Any(row => row == null || row.Length != width))
            {
                errors.Add("Map is not rectangular.");
                return errors;
            }

            int height = level.Height;
            int playerCount = 0;
            int boxCount = 0;
            int goalCount = 0;
            bool boundaryClosed = true;
            bool hasUnknownCharacter = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char cell = level.map[y][x];

                    if (!XsbParser.IsKnown(cell))
                    {
                        hasUnknownCharacter = true;
                        errors.Add($"Unknown XSB character '{cell}' at column {x}, row {y}.");
                    }

                    if (XsbParser.IsPlayer(cell))
                        playerCount++;

                    if (XsbParser.IsBox(cell))
                        boxCount++;

                    if (XsbParser.IsGoal(cell))
                        goalCount++;

                    bool isBoundary = y == 0 || y == height - 1 || x == 0 || x == width - 1;
                    if (isBoundary && cell != XsbParser.Wall)
                        boundaryClosed = false;
                }
            }

            if (playerCount != 1)
                errors.Add($"Expected exactly one player, found {playerCount}.");

            if (boxCount != goalCount)
                errors.Add($"Box count ({boxCount}) does not equal goal count ({goalCount}).");

            if (boxCount == 0)
                errors.Add("Expected at least one box and at least one goal.");

            if (!boundaryClosed)
                errors.Add("Boundary is not fully enclosed by walls.");

            // Reachability needs a single well-defined start and a fully valid XSB grid.
            if (playerCount == 1 && !hasUnknownCharacter)
                AddReachabilityErrors(level, errors);

            return errors;
        }

        private static void AddReachabilityErrors(LevelData level, List<string> errors)
        {
            GridPosition start = level.PlayerPosition ?? new GridPosition(0, 0);

            // ① Player connectivity: walls block, boxes are ignored. Every non-wall cell
            // must be reachable from the player's start.
            HashSet<GridPosition> reachableIgnoringBoxes = FloodFill(level, start, false);
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    if (level.map[y][x] != XsbParser.Wall
                        && !reachableIgnoringBoxes.Contains(new GridPosition(x, y)))
                    {
                        errors.Add("Not all non-wall cells are reachable from the player.");
                        return;
                    }
                }
            }

            // ② Box reachability: walls and boxes block. For each box the player must be
            // able to stand on at least one adjacent non-wall cell. This is the standard
            // static reachability heuristic; full solvability is deferred to the Solver.
            HashSet<GridPosition> reachableWithBoxes = FloodFill(level, start, true);
            foreach (GridPosition box in level.BoxPositions)
            {
                if (!HasReachableNeighbor(level, box, reachableWithBoxes))
                    errors.Add($"Box at column {box.X}, row {box.Y} is unreachable by the player.");
            }
        }

        private static bool HasReachableNeighbor(LevelData level, GridPosition box, HashSet<GridPosition> reachable)
        {
            foreach (GridPosition neighbor in Neighbors(box, level.Width, level.Height))
            {
                if (level.map[neighbor.Y][neighbor.X] != XsbParser.Wall && reachable.Contains(neighbor))
                    return true;
            }

            return false;
        }

        private static HashSet<GridPosition> FloodFill(LevelData level, GridPosition start, bool boxesBlock)
        {
            var visited = new HashSet<GridPosition>();
            int width = level.Width;
            int height = level.Height;

            if (start.X < 0 || start.Y < 0 || start.X >= width || start.Y >= height)
                return visited;

            if (level.map[start.Y][start.X] == XsbParser.Wall)
                return visited;

            HashSet<GridPosition> boxSet = null;
            if (boxesBlock)
                boxSet = new HashSet<GridPosition>(level.BoxPositions);

            var queue = new Queue<GridPosition>();
            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                GridPosition current = queue.Dequeue();

                foreach (GridPosition neighbor in Neighbors(current, width, height))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    char cell = level.map[neighbor.Y][neighbor.X];
                    if (cell == XsbParser.Wall)
                        continue;

                    if (boxesBlock && boxSet.Contains(neighbor))
                        continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return visited;
        }

        private static IEnumerable<GridPosition> Neighbors(GridPosition position, int width, int height)
        {
            if (position.X > 0)
                yield return new GridPosition(position.X - 1, position.Y);
            if (position.X + 1 < width)
                yield return new GridPosition(position.X + 1, position.Y);
            if (position.Y > 0)
                yield return new GridPosition(position.X, position.Y - 1);
            if (position.Y + 1 < height)
                yield return new GridPosition(position.X, position.Y + 1);
        }
    }
}
