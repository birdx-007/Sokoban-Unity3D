using System;
using System.Collections.Generic;
using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Reads the ordered level <see cref="TextAsset"/> references from a <see cref="LevelList"/>
    /// and exposes them through <see cref="ILevelRepository"/>. Corrupt or missing entries are
    /// skipped with an error log so they never surface as playable levels.
    /// </summary>
    public class LevelListRepository : ILevelRepository
    {
        private readonly List<string> ids = new List<string>();
        private readonly List<string> names = new List<string>();
        private readonly List<LevelData> levels = new List<LevelData>();

        public LevelListRepository(LevelList list)
        {
            Load(list);
        }

        public int Count => levels.Count;

        public string GetLevelId(int index)
        {
            ThrowIfOutOfRange(index);
            return ids[index];
        }

        public string GetLevelName(int index)
        {
            ThrowIfOutOfRange(index);
            string name = names[index];
            return string.IsNullOrEmpty(name) ? ids[index] : name;
        }

        public LevelData GetLevel(int index)
        {
            ThrowIfOutOfRange(index);
            return levels[index];
        }

        private void Load(LevelList list)
        {
            if (list == null)
            {
                Debug.LogError("LevelListRepository: LevelList is null; no levels are available.");
                return;
            }

            foreach (TextAsset asset in list.Levels)
            {
                if (asset == null)
                {
                    Debug.LogWarning("LevelListRepository: skipping a null level reference.");
                    continue;
                }

                try
                {
                    LevelData level = LevelJson.FromJson(asset.text);
                    if (level == null)
                        throw new InvalidOperationException("parsed to null.");

                    ids.Add(asset.name);
                    names.Add(level.name);
                    levels.Add(level);
                }
                catch (Exception e)
                {
                    Debug.LogError($"LevelListRepository: skipping bad level '{asset.name}': {e}");
                }
            }
        }

        private void ThrowIfOutOfRange(int index)
        {
            if (index < 0 || index >= levels.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"No level at index {index}.");
        }
    }
}
