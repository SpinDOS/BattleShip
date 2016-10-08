using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class SimulatedPlayer : Player
    {
        protected SimulatedPlayer(Field field) : base(field) { }
        public sealed override void EnemyDisconnected(bool active)
        {
            base.EnemyDisconnected(active);
        }

        protected sealed override void EndGame(bool win)
        {
            base.EndGame(win);
        }

        protected sealed override void MarkSquareWithStatus(Square square, SquareStatus status, bool myField)
        {
            base.MarkSquareWithStatus(square, status, myField);
        }
    }
}
