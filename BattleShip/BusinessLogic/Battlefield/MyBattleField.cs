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
        public MyBattleField(IEnumerable<Square> squares) : base(squares) 
        { }

        public SquareStatus Shot(Square square, Identifier id)
        {
            SquareStatus status = this[square];
            switch (status)
            {
                case SquareStatus.Empty:
                    return SquareStatus.Miss;
                case SquareStatus.Full:
                    break;
                case SquareStatus.Miss:
                case SquareStatus.Hurt:
                case SquareStatus.Dead:
                    throw new ArgumentException("This square is already shot");
            }

            SetStatus(square, SquareStatus.Hurt, id);
            Ship ship = FindShipBySquare(square);
            if (ship.InnerSquares().Any(sq => this[sq] == SquareStatus.Full))
                return SquareStatus.Hurt;

            SetStatus(square, SquareStatus.Dead, id);
            return SquareStatus.Dead;
        }
    }
}
