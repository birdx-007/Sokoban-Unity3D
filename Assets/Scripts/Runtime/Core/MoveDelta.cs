namespace BoxPushing.Core
{
    /// <summary>
    /// The dynamic-state delta produced by a single successful move. Recorded by
    /// <see cref="UndoSystem"/> so the move can be undone and redone.
    /// </summary>
    public readonly struct MoveDelta
    {
        public readonly GridPosition PlayerFrom;
        public readonly GridPosition PlayerTo;
        public readonly GridPosition? BoxFrom;
        public readonly GridPosition? BoxTo;
        public readonly Direction FacingFrom;
        public readonly Direction FacingTo;

        public MoveDelta(
            GridPosition playerFrom,
            GridPosition playerTo,
            GridPosition? boxFrom,
            GridPosition? boxTo,
            Direction facingFrom,
            Direction facingTo)
        {
            PlayerFrom = playerFrom;
            PlayerTo = playerTo;
            BoxFrom = boxFrom;
            BoxTo = boxTo;
            FacingFrom = facingFrom;
            FacingTo = facingTo;
        }

        /// <summary>True when this move pushed a box.</summary>
        public bool PushedBox => BoxFrom.HasValue;

        /// <summary>
        /// Returns the inverse delta describing the reverse transition (undo). Swaps
        /// player/box from-to pairs and facing from-to so the same animation path works.
        /// </summary>
        public MoveDelta Reversed() => new MoveDelta(
            PlayerTo,
            PlayerFrom,
            BoxTo,
            BoxFrom,
            FacingTo,
            FacingFrom);
    }
}
