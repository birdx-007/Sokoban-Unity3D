namespace BoxPushing.Core
{
    /// <summary>
    /// A reversible command over a <see cref="GameState"/>. Execute applies the forward
    /// diff (used for redo), Undo applies the reverse diff.
    /// </summary>
    public interface ICommand
    {
        void Execute(GameState state);
        void Undo(GameState state);
    }
}
