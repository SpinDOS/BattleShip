using System;

namespace BattleShip.Shared
{

    public sealed class Ship
    {
        public Square Start { get; }
        public Square End { get; }
        public byte Length { get; }
        public Ship(Square square)
        {
            Start = End = square;
            Length = 1;
        }

        public Ship(Square start, Square end)
        {
            if (start.X == end.X) // horizontal
            {
                if (start.Y <= end.Y) // all ok
                {
                    Start = start;
                    End = end;
                }
                else // invert start and end
                {
                    End = start;
                    Start = end;
                }
                Length = (byte)(End.Y - Start.Y + 1);
            }
            else if (start.Y == end.Y) // vertical
            {
                if (start.X <= end.X) // all ok
                {
                    Start = start;
                    End = end;
                }
                else // invert start and end
                {
                    End = start;
                    Start = end;
                }
                Length = (byte) (End.X - Start.X + 1);
            }
            else // not horizontal or vertical
                throw new ArgumentException("Squares must be in line");

            // check length for too long ship
            if (Length > 4)
                throw new ArgumentException("Max length of ship is 4");
        }

        public static bool operator ==(Ship left, Ship right)
        {
            if (ReferenceEquals(left, null))
                throw new ArgumentNullException(nameof(left));
            if (ReferenceEquals(right, null))
                throw new ArgumentNullException(nameof(right));
            return left.Start == right.Start && left.End == right.End;
        }

        public static bool operator !=(Ship left, Ship right) => !(left == right);
        public override bool Equals(object obj)
        {
            Ship ship = obj as Ship;
            if (ship == null)
                return false;
            return ship == this;
        }

        public override int GetHashCode() 
            => Start.GetHashCode() << 16 + End.GetHashCode();

        public override string ToString() => $"Ship from {Start} to {End}";
        
    }
}
