using System;

namespace BoxPushing.Core
{
    /// <summary>Persistent completion/progress record for a single level.</summary>
    [Serializable]
    public class LevelSaveData
    {
        public string id;
        public bool completed;
        public int bestMoves;
        public int bestPushes;

        /// <summary>In-progress run, or null when the level is not currently being played.</summary>
        public RunSnapshot inProgress;
    }

    /// <summary>
    /// Root save model. Serialized to JSON via <see cref="SaveJson"/>; file I/O lives in
    /// the Gameplay layer so Core stays free of Unity file APIs.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public LevelSaveData[] levels = Array.Empty<LevelSaveData>();
    }
}
