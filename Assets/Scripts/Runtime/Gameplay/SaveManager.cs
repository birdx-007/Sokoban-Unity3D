using System;
using System.Collections.Generic;
using System.IO;
using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Persistent save store. Holds an in-memory <see cref="SaveData"/> and writes it to
    /// <c>persistentDataPath/save.json</c> on every mutation. Lives on the Persistent
    /// scene and is marked <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private SaveData save;
        private string filePath;

        public string FilePath => filePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            filePath = Path.Combine(Application.persistentDataPath, "save.json");
            Load();
        }

        /// <summary>Gets (creating if needed) the save record for a level.</summary>
        public LevelSaveData GetEntry(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
                throw new ArgumentException("Level id must not be empty.", nameof(levelId));

            return GetOrCreate(levelId);
        }

        public bool IsCompleted(string levelId) => GetOrCreate(levelId).completed;

        public RunSnapshot GetProgress(string levelId) => GetOrCreate(levelId).inProgress;

        public bool HasProgress(string levelId) => GetProgress(levelId) != null;

        /// <summary>Marks a level completed, keeping the smaller of the previous/current bests.</summary>
        public void RecordWin(string levelId, RunSnapshot run)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));

            LevelSaveData entry = GetOrCreate(levelId);
            bool firstCompletion = !entry.completed;

            if (firstCompletion || run.moves < entry.bestMoves)
                entry.bestMoves = run.moves;
            if (firstCompletion || run.pushes < entry.bestPushes)
                entry.bestPushes = run.pushes;

            entry.completed = true;
            entry.inProgress = null;
            Save();
        }

        /// <summary>Stores an in-progress run snapshot for a level.</summary>
        public void RecordProgress(string levelId, RunSnapshot run)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));

            LevelSaveData entry = GetOrCreate(levelId);
            entry.inProgress = run;
            Save();
        }

        /// <summary>Marks a level completed without changing its best scores (editor unlock helper).</summary>
        public void UnlockLevel(string levelId)
        {
            GetOrCreate(levelId).completed = true;
            Save();
        }

        /// <summary>Discards any stored in-progress run for a level.</summary>
        public void ClearProgress(string levelId)
        {
            LevelSaveData entry = GetOrCreate(levelId);
            if (entry.inProgress != null)
            {
                entry.inProgress = null;
                Save();
            }
        }

        private LevelSaveData GetOrCreate(string levelId)
        {
            if (save.levels != null)
            {
                foreach (LevelSaveData entry in save.levels)
                {
                    if (entry != null && entry.id == levelId)
                        return entry;
                }
            }

            var created = new LevelSaveData { id = levelId };
            var list = new List<LevelSaveData>(save.levels ?? Array.Empty<LevelSaveData>()) { created };
            save.levels = list.ToArray();
            return created;
        }

        private void Load()
        {
            if (!File.Exists(filePath))
            {
                save = new SaveData();
                return;
            }

            try
            {
                save = SaveJson.FromJson(File.ReadAllText(filePath)) ?? new SaveData();
                if (save.levels == null)
                    save.levels = Array.Empty<LevelSaveData>();
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: failed to load save at {filePath}: {e}");
                save = new SaveData();
            }
        }

        private void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, SaveJson.ToJson(save));
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: failed to write save at {filePath}: {e}");
            }
        }
    }
}
