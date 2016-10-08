using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public sealed class Field
    {
        public IEnumerable<Square> ShipSquares { get; }

        private Field(IEnumerable<Square> shipSquares)
        {
            ShipSquares = shipSquares;
        }

        public static Field Validate(IEnumerable<Square> shipSquares)
        {
            if (shipSquares == null)
                throw new ArgumentNullException(nameof(shipSquares));
            AggregateException exception = new AggregateException("Bad squares");
            List<Ship> ships = new List<Ship>(10);
            foreach (var square in shipSquares)
            {
                bool added = false;
                foreach (var ship in ships)
                {
                    if (ship.IsSquareNearShip(square))
                    {
                        if (added)
                            throw exception;
                        else
                        {
                            if (ship.TryAddSquare(square))
                                added = true;
                            else
                                throw exception;
                        }
                    }
                    else if (ship.IsShipContainsSquare(square))
                        throw exception;
                }
                if (!added)
                {
                    if (ships.Count == 10)
                        throw exception;
                    ships.Add(new Ship(square));
                }
            }
            if (ships.Count != 10)
                throw exception;
            int s1 = 0, s2 = 0, s3 = 0, s4 = 0;
            foreach (var ship in ships)
            {
                if (ship.Length == 4)
                    if (s4 == 1)
                        throw exception;
                    else
                        s4++;
                else if (ship.Length == 3)
                    if (s3 == 2)
                        throw exception;
                    else
                        s3++;
                else if (ship.Length == 2)
                    if (s2 == 3)
                        throw exception;
                    else
                        s2++;
                else if (ship.Length == 1)
                    if (s1 == 4)
                        throw exception;
                    else
                        s1++;
            }
            return new Field(shipSquares);
        }

        public static Field RandomizeSquares()
        {
            List<Ship> ships = new List<Ship>(10);
            ships.Add(RandomShip(4));
            for (int i = 0; i < 2; i++)
            {
                Ship ship;
                do
                {
                    ship = RandomShip(3);
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
                    ship = RandomShip(2);
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
                    ship = new Ship(new Square((byte)rnd.Next(0, 10), (byte)rnd.Next(0, 10)));
                } while (ships.Any(s => s.IsSquareNearShip(ship.Start) || //while ships contains
                        s.IsShipContainsSquare(ship.Start))); //ship (s) that crosses ship(ship)
                ships.Add(ship);
            }

            var squares = from ship in ships
                from square in ship.InnerSquares()
                select square;
            return new Field(squares);
        }

        private static Ship RandomShip(int length)
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
                    throw new AggregateException("Direction enum has been changed");
            }
            return new Ship(start, end);
        }
    }
}
