using System;
using System.Collections.Generic;
using System.Linq;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{
    public static class ShipExtensions
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

        public static IEnumerable<Square> NearSquares(this Ship ship)
        {
            byte min_x = (byte) (ship.Start.X == 0 ? 0 : ship.Start.X - 1);
            byte max_x = (byte) (ship.End.X == 9 ? 9 : ship.End.X + 1);
            byte min_y = (byte) (ship.Start.Y == 0 ? 0 : ship.Start.Y - 1);
            byte max_y = (byte) (ship.End.Y == 9 ? 9 : ship.End.Y + 1);

            if (min_x != ship.Start.X)
                for (byte j = min_y; j <= max_y; j++)
                    yield return new Square(min_x, j);
            if (max_x != ship.End.X)
                for (byte j = min_y; j <= max_y; j++)
                    yield return new Square(max_x, j);
            if (min_y != ship.Start.Y)
                for (byte i = ship.Start.X; i <= ship.End.X; i++)
                    yield return new Square(i, min_y);
            if (max_y != ship.End.Y)
                for (byte i = ship.Start.X; i <= ship.End.X; i++)
                    yield return new Square(i, max_y);
        }

    }
}
