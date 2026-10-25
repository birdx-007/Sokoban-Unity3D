using System;

namespace BoxPushing.Core
{
    /// <summary>
    /// 2D grid coordinates. X is the column (left to right), Y is the row (top to bottom).
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int X;
        public readonly int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        /// <summary>Position one step in the given direction.</summary>
        public GridPosition Step(Direction direction)
        {
            GridPosition offset = direction.ToOffset();
            return new GridPosition(X + offset.X, Y + offset.Y);
        }

        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);
    }
}
