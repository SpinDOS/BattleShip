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
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));
            return left.Start == right.Start && left.End == right.End;
        }

        public static bool operator !=(Ship left, Ship right) => !(left == right);
    }
}
