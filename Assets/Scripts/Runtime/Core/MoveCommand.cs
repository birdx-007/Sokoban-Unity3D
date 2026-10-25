using System;

namespace BoxPushing.Core
{
    /// <summary>
    /// Undo/redo command for a single successful move. Stores only the one-step
    /// dynamic-state diff: player from/to, optional box from/to, and facing from/to.
    /// </summary>
    public sealed class MoveCommand : ICommand
    {
        private readonly MoveDelta delta;

        public MoveCommand(MoveDelta delta)
        {
            this.delta = delta;
        }

        /// <summary>The forward delta this command applies on execute.</summary>
        public MoveDelta Delta => delta;

        public void Execute(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (delta.BoxFrom.HasValue)
                state.MoveBox(delta.BoxFrom.Value, delta.BoxTo.Value);

            state.SetPlayerPosition(delta.PlayerTo);
            state.SetFacing(delta.FacingTo);
            state.RecordMove(delta.PushedBox);
        }

        public void Undo(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (delta.BoxFrom.HasValue)
                state.MoveBox(delta.BoxTo.Value, delta.BoxFrom.Value);

            state.SetPlayerPosition(delta.PlayerFrom);
            state.SetFacing(delta.FacingFrom);
            state.UnrecordMove(delta.PushedBox);
        }
    }
}
