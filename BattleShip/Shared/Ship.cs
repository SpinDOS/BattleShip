using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
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
            if (start.X == end.X)
            {
                if (start.Y <= end.Y)
                {
                    Start = start;
                    End = end;
                }
                else
                {
                    End = start;
                    Start = end;
                }
                Length = (byte)(End.Y - Start.Y + 1);
            }
            else if (start.Y == end.Y)
            {
                if (start.X <= end.X)
                {
                    Start = start;
                    End = end;
                }
                else
                {
                    End = start;
                    Start = end;
                }
                Length = (byte) (End.X - Start.X + 1);
            }
            else
                throw new ArgumentException("Squares must be in line");
            if (Length > 4)
                throw new ArgumentException("Max length of ship is 4");
        }

        public static bool operator ==(Ship left, Ship right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));
            return left.Start == right.Start && left.End == right.End;
        }

        public static bool operator !=(Ship left, Ship right) => !(left == right);
    }
}
