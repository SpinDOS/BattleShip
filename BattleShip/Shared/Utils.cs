using System.Collections.Generic;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{
    public static class Utils
    {
        public static bool IsSquareNearShip(this Ship ship, Square square) =>
            !ship.IsShipContainsSquare(square)
            && ship.Start.X <= square.X + 1 && ship.End.X + 1 >= square.X
            && ship.Start.Y <= square.Y + 1 && ship.End.Y + 1 >= square.Y;

        public static bool CanAddSquareToShip(this Ship ship, Square square) =>
            (ship.Start.X == ship.End.X && ship.Start.X == square.X
            && (ship.Start.Y == square.Y + 1 || ship.End.Y + 1 == square.Y)) ||
            (ship.Start.Y == ship.End.Y && ship.Start.Y == square.Y
            && (ship.Start.X == square.X + 1 || ship.End.X + 1 == square.X));

        public static bool IsShipContainsSquare(this Ship ship, Square square) =>
            (ship.Start.X == ship.End.X && ship.Start.X == square.X
            && ship.Start.Y <= square.Y && ship.End.Y >= square.Y) ||
            (ship.Start.Y == ship.End.Y && ship.Start.Y == square.Y
            && ship.Start.X <= square.X && ship.End.X >= square.X);

        public static bool IsNearShip(this Ship ship, Ship anotherShip) =>
            ship.IsSquareNearShip(anotherShip.Start) || ship.IsShipContainsSquare(anotherShip.Start) 
            || ship.IsSquareNearShip(anotherShip.End) || ship.IsShipContainsSquare(anotherShip.End);

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
