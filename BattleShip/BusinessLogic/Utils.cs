using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        public static IEnumerable<Square> RandomizeSquares()
        {
            List<Ship> ships = new List<Ship>(10);
            ships.Add(Utils.RandomShip(4));
            for (int i = 0; i < 2; i++)
            {
                Ship ship;
                do
                {
                    ship = Utils.RandomShip(3);
                } while (ships.Any(s => s.IsSquareNearShip(ship.Start) || //while ships contains
                        s.IsShipContainsSquare(ship.Start) || //ship (s) that crosses ship(ship)
                        s.IsSquareNearShip(ship.End) ||
                        s.IsShipContainsSquare(ship.End)));
                ships.Add(ship);
            }
            for (int i = 0; i < 3; i++)
            {
                Ship ship;
                do
                {
                    ship = Utils.RandomShip(2);
                } while (ships.Any(s => s.IsSquareNearShip(ship.Start) || //while ships contains
                        s.IsShipContainsSquare(ship.Start) || //ship (s) that crosses ship(ship)
                        s.IsSquareNearShip(ship.End) ||
                        s.IsShipContainsSquare(ship.End)));
                ships.Add(ship);
            }
            for (int i = 0; i < 4; i++)
            {
                Random rnd = new Random();
                Ship ship;
                do
                {
                    ship = new Ship(new Square((byte) rnd.Next(0, 10), (byte)rnd.Next(0, 10)));
                } while (ships.Any(s => s.IsSquareNearShip(ship.Start) || //while ships contains
                        s.IsShipContainsSquare(ship.Start))); //ship (s) that crosses ship(ship)
                ships.Add(ship);
            }
            foreach (var ship in ships)
                foreach (var square in ship.InnerSquares())
                    yield return square;
        }

        public static Ship RandomShip(int length)
        {
            if (length < 1 || length > 4)
                throw new ArgumentOutOfRangeException(nameof(length));
            Random rnd = new Random();
            Square start = new Square((byte)rnd.Next(length - 1, 10 - length + 1),
                (byte)rnd.Next(length - 1, 10 - length + 1));
            Square end;
            Direction dir = (Direction)rnd.Next(4);
            switch (dir)
            {
                case Direction.Up:
                    end = new Square((byte)(start.X - (length - 1)), start.Y);
                    break;
                case Direction.Down:
                    end = new Square((byte)(start.X + (length - 1)), start.Y);
                    break;
                case Direction.Left:
                    end = new Square(start.X, (byte)(start.Y - (length - 1)));
                    break;
                case Direction.Right:
                    end = new Square(start.X, (byte)(start.Y + (length - 1)));
                    break;
                default:
                    throw new AggregateException("Direction enum has changed");
            }
            return new Ship(start, end);
        }

    }
}
