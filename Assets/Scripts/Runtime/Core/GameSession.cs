using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxPushing.Core
{
    /// <summary>
    /// Owns a full playable run: the mutable <see cref="GameState"/> plus its
    /// <see cref="UndoSystem"/>. This is the single entry point Gameplay uses to drive
    /// moves, undo/redo, reset, and save/restore a run.
    /// </summary>
    public sealed class GameSession
    {
        public GameState State { get; }
        public UndoSystem UndoSystem { get; }

        public GameSession(LevelData level)
        {
            State = new GameState(level);
            UndoSystem = new UndoSystem();
        }

        public GameSession(LevelData level, Direction initialFacing)
        {
            State = new GameState(level, initialFacing);
            UndoSystem = new UndoSystem();
        }

        public bool IsWon => State.IsWon;
        public bool CanUndo => UndoSystem.CanUndo;
        public bool CanRedo => UndoSystem.CanRedo;

        /// <summary>
        /// Attempts a move. On success the command is recorded for undo and the forward
        /// delta is returned. On a blocked attempt the state is unchanged and the delta is
        /// <c>default</c>.
        /// </summary>
        public (MoveResult Result, MoveDelta Delta) TryMove(Direction direction)
        {
            MoveResult result = SokobanRules.TryMove(State, direction, out MoveDelta delta);
            if (result.Success)
                UndoSystem.Record(new MoveCommand(delta));

            return (result, delta);
        }

        /// <summary>Undoes the last move. Returns the reverse delta to animate, or null.</summary>
        public MoveDelta? Undo() => UndoSystem.Undo(State);

        /// <summary>Redoes the last undone move. Returns the forward delta to animate, or null.</summary>
        public MoveDelta? Redo() => UndoSystem.Redo(State);

        /// <summary>Restores the level's initial layout and clears undo/redo history.</summary>
        public void Reset()
        {
            State.Reset();
            UndoSystem.Clear();
        }

        /// <summary>Captures the current dynamic state and undo/redo stacks for saving.</summary>
        public RunSnapshot ToSnapshot()
        {
            return new RunSnapshot
            {
                playerX = State.PlayerPosition.X,
                playerY = State.PlayerPosition.Y,
                facing = State.Facing,
                moves = State.Moves,
                pushes = State.Pushes,
                boxes = State.BoxPositions.Select(GridPositionDto.From).ToArray(),
                undoStack = UndoSystem.ExportUndoStack().Select(MoveDeltaDto.From).ToArray(),
                redoStack = UndoSystem.ExportRedoStack().Select(MoveDeltaDto.From).ToArray()
            };
        }

        /// <summary>Restores dynamic state and undo/redo stacks from a previously captured snapshot.</summary>
        public void Restore(RunSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            IEnumerable<GridPosition> boxes = snapshot.boxes != null
                ? snapshot.boxes.Select(dto => dto.ToGridPosition())
                : Enumerable.Empty<GridPosition>();

            State.Restore(
                new GridPosition(snapshot.playerX, snapshot.playerY),
                snapshot.facing,
                snapshot.moves,
                snapshot.pushes,
                boxes);

            UndoSystem.Restore(
                snapshot.undoStack?.Select(dto => dto.ToMoveDelta()).ToArray(),
                snapshot.redoStack?.Select(dto => dto.ToMoveDelta()).ToArray());
        }

        /// <summary>
        /// Restores from <paramref name="snapshot"/> only when it describes a valid state
        /// for the current level (player/boxes in-bounds, on walkable cells, and the box
        /// count matches). Returns false and leaves the fresh state untouched otherwise.
        /// </summary>
        public bool TryRestore(RunSnapshot snapshot)
        {
            if (snapshot == null || !IsSnapshotValid(snapshot))
                return false;

            Restore(snapshot);
            return true;
        }

        private bool IsSnapshotValid(RunSnapshot snapshot)
        {
            LevelData level = State.Level;
            if (level == null)
                return false;

            GridPosition player = new GridPosition(snapshot.playerX, snapshot.playerY);
            if (!IsWalkable(level, player))
                return false;

            if (snapshot.boxes == null || snapshot.boxes.Length != level.BoxPositions.Count)
                return false;

            var seen = new HashSet<GridPosition>();
            foreach (GridPositionDto box in snapshot.boxes)
            {
                if (box == null)
                    return false;

                GridPosition position = box.ToGridPosition();
                if (!IsWalkable(level, position) || position == player || !seen.Add(position))
                    return false;
            }

            return true;
        }

        private static bool IsWalkable(LevelData level, GridPosition position)
            => level.TryGetCell(position.X, position.Y, out char cell) && cell != XsbParser.Wall;
    }
}
