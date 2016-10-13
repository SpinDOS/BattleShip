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
        public MyBattleField(ClearField myClearField)
        {
            if (myClearField == null)
                throw new ArgumentNullException(nameof(myClearField));
            foreach (var square in myClearField.ShipSquares)
                this[square] = SquareStatus.Full;
        }

        public SquareStatus GetResultOfShot(Square square)
        {
            SquareStatus current = this[square];
            if (current != SquareStatus.Full && current != SquareStatus.Empty)
                throw new AggregateException("You have already shot at the square");

            if (current == SquareStatus.Empty)
                return SquareStatus.Miss;

            Ship ship = FindShipBySquare(square);
            this[square] = SquareStatus.Hurt;
            bool isAlive = IsShipAlive(ship);
            this[square] = SquareStatus.Full;

            return isAlive ? SquareStatus.Hurt : SquareStatus.Dead;
        }

        public sealed override void SetStatusOfSquare(Square square, SquareStatus squareStatus)
        {
            if (squareStatus == SquareStatus.Full)
                throw new AggregateException("I cannot set status Full");
            if (squareStatus == SquareStatus.Hurt && this[square] != SquareStatus.Full)
                throw new AggregateException("This square does not contain ship");
            base.SetStatusOfSquare(square, squareStatus);
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
