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
        {
            // check squares to prevent ships cross and check ships count
            if (!base.Validate(true))
                throw new ArgumentException("Bad squares");
        }

        /// <summary>
        /// Return status of target square after shot
        /// </summary>
        /// <param name="square">target square</param>
        /// <param name="id">owner's id</param>
        /// <returns></returns>
        public SquareStatus Shot(Square square, Identifier id)
        {
            // check current status
            SquareStatus status = this[square];
            switch (status)
            {
                // Empty -> miss
                case SquareStatus.Empty:
                    SetStatus(square, SquareStatus.Miss, id);
                    return SquareStatus.Miss;
                // Full -> Hurt/Dead
                case SquareStatus.Full:
                    break;
                // cant change these statuses
                case SquareStatus.Miss:
                case SquareStatus.Hurt:
                case SquareStatus.Dead:
                    throw new ArgumentException("This square is already shot");
            }

            // set hurt of target square
            SetStatus(square, SquareStatus.Hurt, id);

            // check if ship is dead
            Ship ship = FindShipBySquare(square);
            if (ship.InnerSquares().Any(sq => this[sq] == SquareStatus.Full))
                return SquareStatus.Hurt;

            // mark as dead
            SetStatus(square, SquareStatus.Dead, id);
            return SquareStatus.Dead;
        }
    }
}
