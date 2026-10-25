using BoxPushing.Core;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Ordered list of playable levels. Indices are stable within a build; level ids are
    /// used as save keys.
    /// </summary>
    public interface ILevelRepository
    {
        int Count { get; }

        /// <summary>Stable save key for the level at <paramref name="index"/>.</summary>
        string GetLevelId(int index);

        /// <summary>Display name for the level at <paramref name="index"/>.</summary>
        string GetLevelName(int index);

        /// <summary>Parsed level data for the level at <paramref name="index"/>.</summary>
        LevelData GetLevel(int index);
    }
}
