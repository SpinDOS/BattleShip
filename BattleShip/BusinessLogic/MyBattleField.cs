using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public sealed class MyBattleField : BattleField
    {
        public MyBattleField(Field myField)
        {
            if (myField == null)
                throw new ArgumentNullException(nameof(myField));
            foreach (var square in myField.ShipSquares)
                this[square] = SquareStatus.Full;
        }

        public SquareStatus Shot(Square square)
        {
            SquareStatus current = this[square];
            if (current != SquareStatus.Full || current != SquareStatus.Empty)
                throw new AggregateException("You have already shot at the square");

            current = current == SquareStatus.Empty
                ? SquareStatus.Miss
                : SquareStatus.Hurt;
            this[square] = current;
            if (current == SquareStatus.Miss)
                return current;

            Ship ship = FindShipBySquare(square);
            if (IsShipAlive(ship))
                return current;

            MarkShipAsDead(ship);
            return SquareStatus.Dead;
        }

        private bool IsShipAlive(Ship ship)
        {
            SquareStatus[] notShip = new[] { SquareStatus.Miss, SquareStatus.Empty };
            foreach (var square in ship.InnerSquares())
                if (notShip.Contains(this[square]))
                    throw new ArgumentException("It is not ship of this field");
                else if (this[square] == SquareStatus.Full)
                    return true;
            return false;
        }
    }
}
