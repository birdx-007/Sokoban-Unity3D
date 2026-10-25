using System.Collections.Generic;
using System.Linq;

namespace BoxPushing.Core
{
    /// <summary>
    /// Mutable play state for a level. Static data (walls, floors, goals) stays on
    /// <see cref="LevelData"/>; this type only owns the dynamic state: player position,
    /// the unordered set of box positions, and the player's facing direction.
    /// </summary>
    public sealed class GameState
    {
        private readonly HashSet<GridPosition> boxes = new HashSet<GridPosition>();
        private readonly Direction initialFacing;

        /// <summary>Static level this state plays on. Not copied.</summary>
        public LevelData Level { get; }

        /// <summary>Current player cell.</summary>
        public GridPosition PlayerPosition { get; private set; }

        /// <summary>Player's facing direction after the last successful move.</summary>
        public Direction Facing { get; private set; }

        /// <summary>Number of successful player moves in this run.</summary>
        public int Moves { get; private set; }

        /// <summary>Number of box pushes in this run.</summary>
        public int Pushes { get; private set; }

        /// <summary>Defensive copy of the current box cells (unordered).</summary>
        public GridPosition[] BoxPositions => boxes.ToArray();

        /// <summary>Creates a state using the default initial facing (Down).</summary>
        public GameState(LevelData level)
            : this(level, Direction.Down)
        {
        }

        /// <summary>Creates a state with an explicit initial facing.</summary>
        public GameState(LevelData level, Direction initialFacing)
        {
            Level = level;
            this.initialFacing = initialFacing;
            Reset();
        }

        /// <summary>
        /// Restores the dynamic state to the level's initial layout. Boxes are rebuilt
        /// from <see cref="LevelData"/>; facing returns to the configured initial facing.
        /// </summary>
        public void Reset()
        {
            boxes.Clear();

            if (Level != null)
            {
                foreach (GridPosition box in Level.BoxPositions)
                    boxes.Add(box);

                PlayerPosition = Level.PlayerPosition ?? new GridPosition(0, 0);
            }
            else
            {
                PlayerPosition = new GridPosition(0, 0);
            }

            Facing = initialFacing;
            Moves = 0;
            Pushes = 0;
        }

        /// <summary>True when every goal cell is occupied by a box.</summary>
        public bool IsWon
        {
            get
            {
                if (Level == null || Level.map == null)
                    return false;

                bool hasGoal = false;
                for (int y = 0; y < Level.map.Length; y++)
                {
                    string row = Level.map[y];
                    if (row == null)
                        continue;

                    for (int x = 0; x < row.Length; x++)
                    {
                        if (!XsbParser.IsGoal(row[x]))
                            continue;

                        hasGoal = true;
                        if (!boxes.Contains(new GridPosition(x, y)))
                            return false;
                    }
                }

                return hasGoal;
            }
        }

        /// <summary>True when the cell is inside the level bounds.</summary>
        public bool IsInside(GridPosition position)
            => Level != null && Level.TryGetCell(position.X, position.Y, out _);

        /// <summary>True when the cell is a wall (or outside the level bounds, which blocks movement).</summary>
        public bool IsWall(GridPosition position)
            => Level == null
                || !Level.TryGetCell(position.X, position.Y, out char cell)
                || cell == XsbParser.Wall;

        /// <summary>True when the cell is a goal (floor or box on goal included).</summary>
        public bool IsGoal(GridPosition position)
            => Level != null && Level.TryGetCell(position.X, position.Y, out char cell) && XsbParser.IsGoal(cell);

        /// <summary>True when a box currently occupies the cell.</summary>
        public bool HasBoxAt(GridPosition position) => boxes.Contains(position);

        // Mutation helpers are internal to Core: SokobanRules and UndoSystem own all
        // state changes. Gameplay only drives them through those public entry points.
        internal void SetPlayerPosition(GridPosition position) => PlayerPosition = position;
        internal void SetFacing(Direction facing) => Facing = facing;

        internal void MoveBox(GridPosition from, GridPosition to)
        {
            boxes.Remove(from);
            boxes.Add(to);
        }

        /// <summary>Applies the forward counter change for one move (used on move/redo).</summary>
        internal void RecordMove(bool pushedBox)
        {
            Moves++;
            if (pushedBox)
                Pushes++;
        }

        /// <summary>Applies the reverse counter change for one move (used on undo).</summary>
        internal void UnrecordMove(bool pushedBox)
        {
            Moves--;
            if (pushedBox)
                Pushes--;
        }

        /// <summary>Overwrites the whole dynamic state (used when restoring a run snapshot).</summary>
        internal void Restore(
            GridPosition playerPosition,
            Direction facing,
            int moves,
            int pushes,
            IEnumerable<GridPosition> boxPositions)
        {
            boxes.Clear();
            if (boxPositions != null)
            {
                foreach (GridPosition box in boxPositions)
                    boxes.Add(box);
            }

            PlayerPosition = playerPosition;
            Facing = facing;
            Moves = moves;
            Pushes = pushes;
        }
    }
}
