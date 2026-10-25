namespace BoxPushing.Core
{
    /// <summary>
    /// Structured outcome of a single move attempt.
    /// </summary>
    public readonly struct MoveResult
    {
        /// <summary>True when the player actually moved.</summary>
        public readonly bool Success;

        /// <summary>True when the move pushed exactly one box.</summary>
        public readonly bool PushedBox;

        /// <summary>True when the move resulted in a win (all goals covered).</summary>
        public readonly bool Won;

        public MoveResult(bool success, bool pushedBox, bool won)
        {
            Success = success;
            PushedBox = pushedBox;
            Won = won;
        }

        /// <summary>A blocked attempt changes nothing.</summary>
        public static MoveResult Blocked => new MoveResult(false, false, false);

        /// <summary>A successful move that may or may not have pushed a box.</summary>
        public static MoveResult Moved(bool pushedBox, bool won) => new MoveResult(true, pushedBox, won);
    }
}
