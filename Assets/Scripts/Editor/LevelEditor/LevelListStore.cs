using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BoxPushing.Core;
using BoxPushing.Gameplay;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

namespace BoxPushing.Editor
{
    /// <summary>
    /// Persistence helpers for the Level Editor: loading/creating <see cref="LevelList"/>,
    /// generating default levels, writing level JSON, and deleting files to the OS recycle bin.
    /// </summary>
    public static class LevelListStore
    {
        public const string LevelListPath = "Assets/Data/LevelList.asset";
        public const string LevelsFolder = "Assets/Data/Levels";
        public const string LevelFileNamePrefix = "Sokoban_Level";

        private static readonly Regex LevelNumberPattern = new Regex(
            "^" + Regex.Escape(LevelFileNamePrefix) + "(\\d+)$",
            RegexOptions.Compiled);

        [InitializeOnLoadMethod]
        private static void AutoCreateLevelList()
        {
            EnsureLevelList();
        }

        /// <summary>Loads the level list asset, creating it at <see cref="LevelListPath"/> when missing.</summary>
        public static LevelList EnsureLevelList()
        {
            LevelList list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelListPath);
            if (list != null)
                return list;

            EnsureFolder("Assets/Data");
            EnsureFolder(LevelsFolder);

            list = ScriptableObject.CreateInstance<LevelList>();
            AssetDatabase.CreateAsset(list, LevelListPath);
            AssetDatabase.SaveAssets();
            return list;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder(parent ?? "Assets", leaf);
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Creates a blank 8x8 level (all floor) as the default starting point.</summary>
        public static LevelData CreateDefaultLevel(string name)
        {
            return new LevelData(
                string.IsNullOrEmpty(name) ? "New Level" : name,
                new[]
                {
                    "        ",
                    "        ",
                    "        ",
                    "        ",
                    "        ",
                    "        ",
                    "        ",
                    "        "
                });
        }

        /// <summary>
        /// Returns the next free <c>Sokoban_LevelNN</c> file name (two-digit padded), one greater
        /// than the largest existing level number.
        /// </summary>
        public static string NextLevelFileName(IEnumerable<string> existingNames)
        {
            int max = 0;
            foreach (string name in existingNames ?? Enumerable.Empty<string>())
            {
                Match match = name != null ? LevelNumberPattern.Match(name) : Match.Empty;
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                    max = Math.Max(max, number);
            }

            return $"{LevelFileNamePrefix}{max + 1:00}";
        }

        /// <summary>
        /// Writes a level to <c>Assets/Data/Levels/{fileName}.json</c> and returns the imported
        /// <see cref="TextAsset"/>.
        /// </summary>
        public static TextAsset WriteLevelJson(string fileName, LevelData level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            EnsureFolder(LevelsFolder);

            string safeName = string.IsNullOrEmpty(fileName) ? "Sokoban_Level" : fileName;
            string path = $"{LevelsFolder}/{safeName}.json";

            File.WriteAllText(path, LevelJson.ToJson(level));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        /// <summary>Persists the ordered level references into the <see cref="LevelList"/> asset.</summary>
        public static void PersistLevelList(LevelList list, IEnumerable<TextAsset> orderedLevels)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            list.SetLevels(orderedLevels);
            EditorUtility.SetDirty(list);
        }

        /// <summary>
        /// Sends a file or directory to the OS recycle bin. Returns true on success; on
        /// non-Windows platforms this falls back to Unity's permanent delete.
        /// </summary>
        public static bool DeleteFileToRecycleBin(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized = path.Replace('/', Path.DirectorySeparatorChar);
            bool exists = File.Exists(normalized) || Directory.Exists(normalized);
            if (!exists)
                return true;

#if UNITY_EDITOR_WIN
            var operation = new ShFileOpStruct
            {
                hwnd = IntPtr.Zero,
                wFunc = FoDelete,
                pFrom = normalized + '\0',
                pTo = null,
                fFlags = FofAllowUndo | FofNoConfirmation | FofNoErrorUi | FofSilent,
                fAnyOperationsAborted = false,
                hNameMappings = IntPtr.Zero,
                lpszProgressTitle = null
            };

            int result = SHFileOperationW(ref operation);
            return result == 0 && !operation.fAnyOperationsAborted;
#else
            return FileUtil.DeleteFileOrDirectory(normalized);
#endif
        }

#if UNITY_EDITOR_WIN
        private const uint FoDelete = 0x0003;
        private const ushort FofSilent = 0x0004;
        private const ushort FofNoConfirmation = 0x0010;
        private const ushort FofAllowUndo = 0x0040;
        private const ushort FofNoErrorUi = 0x0400;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShFileOpStruct
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHFileOperationW(ref ShFileOpStruct lpFileOp);
#endif
    }
}
