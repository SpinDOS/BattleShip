using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public sealed class EnemyBattleField : BattleField
    {
        public void SetStatusOfSquare(Square square, SquareStatus squareStatus)
        {
            if (this[square] != SquareStatus.Empty)
                throw new ArgumentException("Status of this square is already known");
            this[square] = squareStatus;
            if (squareStatus == SquareStatus.Dead)
                MarkShipAsDead(FindShipBySquare(square));
        }
    }
}
