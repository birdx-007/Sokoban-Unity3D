using System;

namespace BoxPushing.Core
{
    /// <summary>
    /// Discrete movement directions on the grid.
    /// </summary>
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Coordinate helpers for <see cref="Direction"/>.
    /// </summary>
    public static class DirectionExtensions
    {
        /// <summary>
        /// Unit grid offset for a direction. X is the column, Y is the row, so
        /// Up=(0,-1), Down=(0,+1), Left=(-1,0), Right=(+1,0).
        /// </summary>
        public static GridPosition ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return new GridPosition(0, -1);
                case Direction.Down:
                    return new GridPosition(0, 1);
                case Direction.Left:
                    return new GridPosition(-1, 0);
                case Direction.Right:
                    return new GridPosition(1, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
