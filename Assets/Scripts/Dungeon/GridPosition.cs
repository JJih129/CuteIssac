using System;

namespace CuteIssac.Dungeon
{
    /// <summary>
    /// Integer grid coordinate used by generated dungeon rooms.
    /// Keep room layout in grid space so generation, lookup, and neighbor tests stay deterministic.
    /// </summary>
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public static GridPosition Zero => new(0, 0);
        public static GridPosition operator +(GridPosition left, GridPosition right) => new(left.X + right.X, left.Y + right.Y);

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
