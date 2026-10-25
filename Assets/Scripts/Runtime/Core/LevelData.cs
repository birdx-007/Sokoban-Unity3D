using System;
using System.Collections.Generic;

namespace BoxPushing.Core
{
    /// <summary>
    /// Level model: XSB character grid plus metadata. Pure data; serialized to JSON via <see cref="LevelJson"/>.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public string name;
        public string[] map;

        public LevelData()
        {
            name = string.Empty;
            map = Array.Empty<string>();
        }

        public LevelData(string name, string[] map)
        {
            this.name = name ?? string.Empty;
            this.map = map ?? Array.Empty<string>();
        }

        /// <summary>Grid width (length of the first row).</summary>
        public int Width => map != null && map.Length > 0 && map[0] != null ? map[0].Length : 0;

        /// <summary>Grid height (row count).</summary>
        public int Height => map != null ? map.Length : 0;

        /// <summary>First player cell, or null when there is none.</summary>
        public GridPosition? PlayerPosition
        {
            get
            {
                var players = PlayerPositions;
                return players.Count > 0 ? players[0] : (GridPosition?)null;
            }
        }

        /// <summary>All player cells ('@' and '+').</summary>
        public IReadOnlyList<GridPosition> PlayerPositions => Collect(XsbParser.IsPlayer);

        /// <summary>All box cells ('$' and '*').</summary>
        public IReadOnlyList<GridPosition> BoxPositions => Collect(XsbParser.IsBox);

        /// <summary>All goal cells ('.', '+' and '*').</summary>
        public IReadOnlyList<GridPosition> GoalPositions => Collect(XsbParser.IsGoal);

        public bool TryGetCell(int x, int y, out char cell)
        {
            if (map == null || y < 0 || y >= map.Length)
            {
                cell = default;
                return false;
            }

            string row = map[y];
            if (row == null || x < 0 || x >= row.Length)
            {
                cell = default;
                return false;
            }

            cell = row[x];
            return true;
        }

        private List<GridPosition> Collect(Func<char, bool> predicate)
        {
            var result = new List<GridPosition>();
            if (map == null)
                return result;

            for (int y = 0; y < map.Length; y++)
            {
                string row = map[y];
                if (row == null)
                    continue;

                for (int x = 0; x < row.Length; x++)
                {
                    if (predicate(row[x]))
                        result.Add(new GridPosition(x, y));
                }
            }

            return result;
        }
    }
}
