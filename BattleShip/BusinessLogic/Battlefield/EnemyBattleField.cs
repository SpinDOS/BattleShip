using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Class for enemy field
    /// </summary>
    public sealed class EnemyBattleField : BattleField
    {
        /// <summary>
        /// Creates battlefield with no ships
        /// </summary>
        public EnemyBattleField() : base(new Square[0])
        { }

        /// <summary>
        /// Shot enemy field by square with status
        /// </summary>
        /// <param name="square">target square</param>
        /// <param name="status">new status</param>
        /// <param name="id">owner's id to identify</param>
        public void Shot(Square square, SquareStatus status, Identifier id)
        {
            // can mark only empty square
            if (this[square] != SquareStatus.Empty)
                throw new AggregateException("This square is already shot");
            // call base method
            base.SetStatus(square, status, id);
        }
    }
}
