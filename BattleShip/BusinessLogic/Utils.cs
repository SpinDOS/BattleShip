using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    internal enum Direction
    {
        Up = 0, // necessary for random
        Down,
        Left, 
        Right,
    }
    internal static class Utils
    {
        public static bool IsSquareNearShip(this Ship ship, Square square) =>
            !ship.IsShipContainsSquare(square)
            && ship.Start.X <= square.X + 1 && ship.End.X + 1 >= square.X
            && ship.Start.Y <= square.Y + 1 && ship.End.Y + 1 >= square.Y;

        public static bool CanAddSquareToShip(this Ship ship, Square square) =>
            (ship.Start.X == ship.End.X && ship.Start.Y == square.Y
            && ship.Start.X == square.X + 1 && ship.End.X + 1 == square.X) ||
            (ship.Start.Y == ship.End.Y && ship.Start.X == square.X
            && ship.Start.Y == square.Y + 1 && ship.End.Y + 1 == square.Y);

        public static bool IsShipContainsSquare(this Ship ship, Square square) =>
            (ship.Start.X == ship.End.X && ship.Start.Y == square.Y 
            && ship.Start.X <= square.X && ship.End.X >= square.X) ||
            (ship.Start.Y == ship.End.Y && ship.Start.X == square.X 
            && ship.Start.Y <= square.Y && ship.End.Y >= square.Y);

        public static IEnumerable<Square> InnerSquares(this Ship ship)
        {
            if (ship.Start.X == ship.End.X)
                for (byte y = ship.Start.Y; y <= ship.End.Y; y++)
                    yield return new Square(ship.Start.X, y);
            else
                for (byte x = ship.Start.X; x <= ship.End.X; x++)
                    yield return new Square(x, ship.Start.Y);
        }
    }
}
