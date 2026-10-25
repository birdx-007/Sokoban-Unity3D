using System;
using UnityEngine;

namespace BoxPushing.Core
{
    /// <summary>JSON encode/decode for <see cref="SaveData"/> using JsonUtility.</summary>
    public static class SaveJson
    {
        public static string ToJson(SaveData save)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));

            return JsonUtility.ToJson(save);
        }

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON must not be null or empty.", nameof(json));

            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}
