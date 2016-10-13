using System;
using System.Collections.Generic;
using System.Linq;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{
    public sealed class ClearField
    {
        public IEnumerable<Square> ShipSquares { get; }

        private ClearField(IEnumerable<Square> shipSquares)
        {
            ShipSquares = shipSquares;
        }

        public static ClearField Validate(IEnumerable<Square> shipSquares)
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
            return new ClearField(shipSquares);
        }

        public static ClearField RandomizeSquares()
        {
            List<Ship> ships = new List<Ship>(10);
            ships.Add(RandomShip(4));
            for (int i = 0; i < 2; i++) // 2 of 3squared ships
            {
                Ship ship;
                do
                    ship = RandomShip(3);
                while (ships.Any(s => s.IsNearShip(ship))); // while some of added ships crosses new ship
                ships.Add(ship);
            }
            for (int i = 0; i < 3; i++) // 3 of 2squared ships
            {
                Ship ship;
                do
                    ship = RandomShip(2);
                while (ships.Any(s => s.IsNearShip(ship))); // while some of added ships crosses new ship
                ships.Add(ship);
            }
            for (int i = 0; i < 4; i++) // 4 of 1squared ships
            {
                Ship ship;
                do
                    ship = RandomShip(1);
                while (ships.Any(s => s.IsNearShip(ship))); // while some of added ships crosses new ship
                ships.Add(ship);
            }

            var squares = from ship in ships
                from square in ship.InnerSquares()
                select square;
            return new ClearField(squares);
        }

        private static Ship RandomShip(int length)
        {
            Random rnd = new Random();
            byte x = (byte) rnd.Next(10), y = (byte) rnd.Next(10);
            if (--length == 0) // 1square ship
                return new Ship(new Square(x, y)); 
            byte min_x = x, max_x = x, min_y = y, max_y = y;
            int direction = rnd.Next(4); // 0 = up, 1 = down, 2 = left, 3 = right
            switch (direction)
            {
                case 0:
                    while (min_x > 0 && max_x - min_x < length) // build up until end
                        --min_x;
                    if (max_x - min_x == length)
                        break;
                    else
                        goto case 1; // build down until length is reached
                case 1:
                    while (max_x < 9 && max_x - min_x < length) // build down until end
                        ++max_x;
                    if (max_x - min_x == length)
                        break;
                    else
                        goto case 0; // build up until length is reached
                case 2:
                    while (min_y > 0 && max_y - min_y < length) // build left until end
                        --min_y;
                    if (max_y - min_y == length)
                        break;
                    else
                        goto case 3; // build right until length is reached
                case 3:
                    while (max_y < 9 && max_y - min_y < length) // build right until end
                        ++max_y;
                    if (max_y - min_y == length)
                        break;
                    else
                        goto case 2; // build left until length is reached
            }
            return new Ship(new Square(min_x, min_y), new Square(max_x, max_y));
        }
    }
}
