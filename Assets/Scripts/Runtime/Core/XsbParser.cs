using System;

namespace BoxPushing.Core
{
    /// <summary>
    /// XSB character semantics: '#' wall, ' ' floor, '.' goal, '@' player, '$' box,
    /// '+' player on goal, '*' box on goal.
    /// </summary>
    public static class XsbParser
    {
        public const char Wall = '#';
        public const char Floor = ' ';
        public const char Goal = '.';
        public const char Player = '@';
        public const char Box = '$';
        public const char PlayerOnGoal = '+';
        public const char BoxOnGoal = '*';

        public static bool IsWall(char c) => c == Wall;
        public static bool IsFloor(char c) => c == Floor;

        /// <summary>True for '.', '+' and '*', which all mark a goal cell.</summary>
        public static bool IsGoal(char c) => c == Goal || c == PlayerOnGoal || c == BoxOnGoal;

        /// <summary>True for '@' and '+'.</summary>
        public static bool IsPlayer(char c) => c == Player || c == PlayerOnGoal;

        /// <summary>True for '$' and '*'.</summary>
        public static bool IsBox(char c) => c == Box || c == BoxOnGoal;

        public static bool IsKnown(char c) =>
            IsWall(c) || IsFloor(c) || IsGoal(c) || IsPlayer(c) || IsBox(c);

        public static bool TryParse(char c, out CellType cellType)
        {
            switch (c)
            {
                case Wall:
                    cellType = CellType.Wall;
                    return true;
                case Floor:
                    cellType = CellType.Floor;
                    return true;
                case Goal:
                    cellType = CellType.Goal;
                    return true;
                case Player:
                    cellType = CellType.Player;
                    return true;
                case Box:
                    cellType = CellType.Box;
                    return true;
                case PlayerOnGoal:
                    cellType = CellType.PlayerOnGoal;
                    return true;
                case BoxOnGoal:
                    cellType = CellType.BoxOnGoal;
                    return true;
                default:
                    cellType = default;
                    return false;
            }
        }

        public static CellType Parse(char c)
        {
            if (TryParse(c, out var cellType))
                return cellType;

            throw new ArgumentException($"Unknown XSB character: '{c}'");
        }

        public static char ToChar(CellType cellType)
        {
            switch (cellType)
            {
                case CellType.Wall:
                    return Wall;
                case CellType.Floor:
                    return Floor;
                case CellType.Goal:
                    return Goal;
                case CellType.Player:
                    return Player;
                case CellType.Box:
                    return Box;
                case CellType.PlayerOnGoal:
                    return PlayerOnGoal;
                case CellType.BoxOnGoal:
                    return BoxOnGoal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cellType), cellType, null);
            }
        }
    }
}
