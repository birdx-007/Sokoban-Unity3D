using System;

namespace BoxPushing.Core
{
    /// <summary>
    /// Sokoban movement rules. Instant and atomic; no animation or timing lives here.
    /// A blocked attempt returns <see cref="MoveResult.Blocked"/> without changing state.
    /// </summary>
    public static class SokobanRules
    {
        /// <summary>Attempts a move, discarding the delta for undo tracking.</summary>
        public static MoveResult TryMove(GameState state, Direction direction)
            => TryMove(state, direction, out _);

        /// <summary>
        /// Attempts a move in <paramref name="direction"/>. On success the state is
        /// mutated and <paramref name="delta"/> describes the change for undo tracking.
        /// </summary>
        public static MoveResult TryMove(GameState state, Direction direction, out MoveDelta delta)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            GridPosition from = state.PlayerPosition;
            GridPosition next = from.Step(direction);

            if (state.IsWall(next))
            {
                delta = default;
                return MoveResult.Blocked;
            }

            Direction facingFrom = state.Facing;
            bool pushedBox = false;
            GridPosition? boxFrom = null;
            GridPosition? boxTo = null;

            if (state.HasBoxAt(next))
            {
                GridPosition beyond = next.Step(direction);
                if (state.IsWall(beyond) || state.HasBoxAt(beyond))
                {
                    delta = default;
                    return MoveResult.Blocked;
                }

                boxFrom = next;
                boxTo = beyond;
                pushedBox = true;
            }

            if (pushedBox)
                state.MoveBox(boxFrom.Value, boxTo.Value);

            state.SetPlayerPosition(next);
            state.SetFacing(direction);
            state.RecordMove(pushedBox);

            delta = new MoveDelta(from, next, boxFrom, boxTo, facingFrom, direction);
            return MoveResult.Moved(pushedBox, state.IsWon);
        }
    }
}
