using System;
using System.Collections.Generic;
using System.Linq;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{
    public static class BattlefieldExtensions
    {
        public static IEnumerable<Square> GetFullSquares(this MyBattleField field)
        {
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                {
                    Square square = new Square(i, j);
                    if (field[square] == SquareStatus.Full)
                        yield return square;
                }
        }


        public static IEnumerable<Square> RandomizeSquares()
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

            foreach (var ship in ships)
                foreach (var Square in ship.InnerSquares())
                    yield return Square;
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
