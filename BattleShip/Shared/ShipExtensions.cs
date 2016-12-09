using System;
using System.Collections.Generic;
using System.Linq;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{
    public static class ShipExtensions
    {
        /// <summary>
        /// Check if square is near to ship
        /// </summary>
        public static bool IsSquareNearShip(this Ship ship, Square square) =>
            !ship.IsShipContainsSquare(square) // not inner
            && ship.Start.X <= square.X + 1 && ship.End.X + 1 >= square.X // horizontal
            && ship.Start.Y <= square.Y + 1 && ship.End.Y + 1 >= square.Y;// vertical

        /// <summary>
        /// Check if square can be added to ship
        /// </summary>
        public static bool CanAddSquareToShip(this Ship ship, Square square) =>
            (ship.Start.X == ship.End.X && ship.Start.X == square.X // horizontal
            && (ship.Start.Y == square.Y + 1 || ship.End.Y + 1 == square.Y)) ||
            (ship.Start.Y == ship.End.Y && ship.Start.Y == square.Y // vertical
            && (ship.Start.X == square.X + 1 || ship.End.X + 1 == square.X));

        /// <summary>
        /// Check if square is placed in the ship
        /// </summary>
        public static bool IsShipContainsSquare(this Ship ship, Square square) =>
            (ship.Start.X == ship.End.X && ship.Start.X == square.X // horizontal
            && ship.Start.Y <= square.Y && ship.End.Y >= square.Y) ||
            (ship.Start.Y == ship.End.Y && ship.Start.Y == square.Y // vertical
            && ship.Start.X <= square.X && ship.End.X >= square.X);

        /// <summary>
        /// Check if ship is near another ship
        /// </summary>
        public static bool IsNearShip(this Ship ship, Ship anotherShip) =>
            ship.IsSquareNearShip(anotherShip.Start) || ship.IsShipContainsSquare(anotherShip.Start) 
            || ship.IsSquareNearShip(anotherShip.End) || ship.IsShipContainsSquare(anotherShip.End);

        /// <summary>
        /// Return squares in the ship
        /// </summary>
        public static IEnumerable<Square> InnerSquares(this Ship ship)
        {
            if (ship.Start.X == ship.End.X) // horizontal
                for (byte y = ship.Start.Y; y <= ship.End.Y; y++)
                    yield return new Square(ship.Start.X, y);
            else // vertical
                for (byte x = ship.Start.X; x <= ship.End.X; x++)
                    yield return new Square(x, ship.Start.Y);
        }

        /// <summary>
        /// Return squares near ship
        /// </summary>
        public static IEnumerable<Square> NearSquares(this Ship ship)
        {
            // initialize border vars
            byte min_x = (byte) (ship.Start.X == 0 ? 0 : ship.Start.X - 1);
            byte max_x = (byte) (ship.End.X == 9 ? 9 : ship.End.X + 1);
            byte min_y = (byte) (ship.Start.Y == 0 ? 0 : ship.Start.Y - 1);
            byte max_y = (byte) (ship.End.Y == 9 ? 9 : ship.End.Y + 1);

            if (min_x != ship.Start.X) // left border
                for (byte j = min_y; j <= max_y; j++)
                    yield return new Square(min_x, j);
            if (max_x != ship.End.X) // right border
                for (byte j = min_y; j <= max_y; j++)
                    yield return new Square(max_x, j);
            if (min_y != ship.Start.Y) // top border
                for (byte i = ship.Start.X; i <= ship.End.X; i++)
                    yield return new Square(i, min_y);
            if (max_y != ship.End.Y) // bottom border
                for (byte i = ship.Start.X; i <= ship.End.X; i++)
                    yield return new Square(i, max_y);
        }

    }
}
