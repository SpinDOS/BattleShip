using System;

namespace BattleShip.Shared
{
    public enum SquareStatus : byte
    {
        Empty = 0, //обязательно для инициализации ClearField
        Miss = 1,
        Full = 2,
        Hurt = 3,
        Dead = 4,
    }

    /// <summary>
    /// Square of field
    /// </summary>
    public struct Square
    {
        public Square(byte x, byte y)
        {
            if (x < 0)
                throw new ArgumentOutOfRangeException("x < 0");
            if (y < 0)
                throw new ArgumentOutOfRangeException("y < 0");
            if (x > 9)
                throw new ArgumentOutOfRangeException("x > 9");
            if (y > 9)
                throw new ArgumentOutOfRangeException("y > 9");
            X = x;
            Y = y;
        }
        public byte X { get; }
        public byte Y { get; }

        public override string ToString() => $"Square {X}, {Y}";
        public override int GetHashCode() => (X << 8) + Y;
        public static bool operator ==(Square l, Square r) => l.X == r.X && l.Y == r.Y;
        public static bool operator !=(Square l, Square r) => l.X != r.X || l.Y != r.Y;
        public override bool Equals(object obj)
        {
            if (!(obj is Square))
                return false;
            return this == (Square) obj;
        }
    }
}
