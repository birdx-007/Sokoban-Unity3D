using System;
using UnityEngine;

namespace BoxPushing.Core
{
    /// <summary>
    /// JSON encode/decode for <see cref="LevelData"/> using JsonUtility (simple schema, no external dependency).
    /// </summary>
    public static class LevelJson
    {
        public static string ToJson(LevelData level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            return JsonUtility.ToJson(level);
        }

        public static LevelData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON must not be null or empty.", nameof(json));

            return JsonUtility.FromJson<LevelData>(json);
        }
    }
}
