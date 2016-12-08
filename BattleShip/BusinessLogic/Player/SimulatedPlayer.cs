using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class SimulatedPlayer : Player
    {
        protected bool? myTurn = null;

        protected SimulatedPlayer(MyBattleField myField) : base(myField)
        { }
        public abstract Square GetNextShot();

        public void SetMeShotFirst(bool meFirst)
        {
            if (myTurn.HasValue)
                throw new AggregateException("Player is already initialized");
            myTurn = meFirst;
        }

        public virtual void GetReportOfMyShot(Square square, SquareStatus status)
        {
            if (!myTurn.HasValue || !myTurn.Value)
                throw new AggregateException("Can not receive report of my shot now");
            myTurn = status != SquareStatus.Miss;
            EnemyField.Shot(square, status, _id);
        }

        public SquareStatus ReportEnemyShotResult(Square square)
        {
            if (!myTurn.HasValue || myTurn.Value)
                throw new AggregateException("Can't receive shot now");
            var status = MyField.Shot(square, _id);
            myTurn = status == SquareStatus.Miss;
            return status;
        }
    }
}
