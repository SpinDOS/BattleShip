using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.BusinessLogic
{
    public class Player
    {
        #region Properties

        private SquareStatus[,] Enemy = new SquareStatus[10, 10];
        private SquareStatus[,] Me = new SquareStatus[10, 10];

        public SquareStatus this[bool isYourField, byte x, byte y] => this[isYourField, new Square(x, y)];
        public SquareStatus this[bool isYourField, Square square] => 
            isYourField? Me[square.X, square.Y]: Enemy[square.X, square.Y];

        #endregion

        #region Creating

        public Player(IEnumerable<Square> ShipSquares)
        {
            if (ShipSquares == null)
                throw new ArgumentNullException(nameof(ShipSquares));
            if (!Validate(ShipSquares))
                throw new ArgumentException("Bad ships");
            foreach (var square in ShipSquares)
                Me[square.X, square.Y] = SquareStatus.Full;
        }

        private bool Validate(IEnumerable<Square> ShipSquares)
        {
            List<Ship> ships = new List<Ship>(10);
            foreach (var square in ShipSquares)
            {
                bool added = false;
                foreach (var ship in ships)
                {
                    if (ship.IsSquareNearShip(square))
                    {
                        if (added)
                            return false;
                        else
                        { if (ship.TryAddSquare(square))
                            added = true;
                        else
                            return false;}
                    }
                    else if (ship.IsShipContainsSquare(square))
                        throw new ArgumentException("Collection has 2 identical squares");
                }
                if (!added)
                {
                    if (ships.Count == 10)
                        return false;
                    ships.Add(new Ship(square));
                }
            }
            if (ships.Count != 10)
                return false;
            int s1 = 0, s2 = 0, s3 = 0, s4 = 0;
            foreach (var ship in ships)
            {
                if (ship.Length == 4)
                    if (s4 == 1)
                        return false;
                    else
                        s4++;
                else if (ship.Length == 3)
                    if (s3 == 2)
                        return false;
                    else
                        s3++;
                else if (ship.Length == 2)
                    if (s2 == 3)
                        return false;
                    else
                        s2++;
                else if (ship.Length == 1)
                    if (s1 == 4)
                        return false;
                    else
                        s1++;
            }
            return true;
        }

        #endregion
    }
}
