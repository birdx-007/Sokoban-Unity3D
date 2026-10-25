using System.Collections.Generic;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Ordered list of level <see cref="TextAsset"/> references. The order is the play
    /// order at runtime and must only be edited through the Level Editor
    /// (Sokoban/Level/LevelEditor); the Inspector shows a read-only view.
    /// </summary>
    [CreateAssetMenu(menuName = "Sokoban/Level List", fileName = "LevelList", order = 0)]
    public class LevelList : ScriptableObject
    {
        [SerializeField] private List<TextAsset> levels = new List<TextAsset>();

        /// <summary>Ordered level assets. Exposed as the live list so the editor can update it in place.</summary>
        public List<TextAsset> Levels => levels;

        public int Count => levels != null ? levels.Count : 0;

        public TextAsset GetAt(int index) => levels[index];

        /// <summary>Replaces the ordered contents (used by the Level Editor when persisting changes).</summary>
        public void SetLevels(IEnumerable<TextAsset> orderedLevels)
        {
            levels = new List<TextAsset>(orderedLevels ?? System.Array.Empty<TextAsset>());
        }
    }
}
