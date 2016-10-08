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

        protected sealed override void MarkSquareWithStatus(Square square, SquareStatus status, bool myField)
        {
            base.MarkSquareWithStatus(square, status, myField);
        }

        public sealed override void SetStatusOfMyShot(Square square, SquareStatus result)
        {
            base.SetStatusOfMyShot(square, result);
        }

        public sealed override SquareStatus ShotFromEnemy(Square square)
        {
            return base.ShotFromEnemy(square);
        }
    }
}
