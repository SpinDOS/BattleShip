using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public class EnemyBattleField : BattleField
    {
        public EnemyBattleField() : base(new Square[0])
        { }

        public void Shot(Square square, SquareStatus status, Identifier id)
        {
            if (this[square] != SquareStatus.Empty)
                throw new AggregateException("This square is already shot");
            base.SetStatus(square, status, id);
        }
    }
}
