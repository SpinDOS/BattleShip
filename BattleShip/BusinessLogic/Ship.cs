using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.BusinessLogic
{
    public sealed class Ship
    {
        public Square Start { get; private set; }
        public Square End { get; private set; }
        public byte Length { get; private set; }
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

        public bool TryAddSquare(Square square)
        {
            if (Length == 4)
                return false;
            if ((Start.X == End.X && Start.X == square.X && Start.Y == square.Y + 1) ||
                (Start.Y == End.Y && Start.Y == square.Y && Start.X == square.X + 1))
            {
                Start = square;
                Length++;
                return true;
            }
            if ((Start.X == End.X && Start.X == square.X && End.Y + 1 == square.Y) ||
                (Start.Y == End.Y && Start.Y == square.Y && End.X + 1 == square.X))
            {
                End = square;
                Length++;
                return true;
            }
            return false;
        }
    }
}
