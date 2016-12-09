using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Not real, simulated player
    /// </summary>
    public abstract class SimulatedPlayer : Player
    {
        protected bool? myTurn = null;

        /// <summary>
        /// Create player with field
        /// </summary>
        protected SimulatedPlayer(MyBattleField myField) : base(myField)
        { }

        /// <summary>
        /// Generates next shot of player
        /// </summary>
        public abstract Square GetNextShot();

        /// <summary>
        /// Initialize player with first turt status
        /// </summary>
        public void SetMeShotFirst(bool meFirst)
        {
            if (myTurn.HasValue)
                throw new AggregateException("Player is already initialized");
            if (IsGameEnded)
                throw new AggregateException("Game ended");
            myTurn = meFirst;
        }

        /// <summary>
        /// Set result of my shot
        /// </summary>
        /// <param name="square">target of shot</param>
        /// <param name="status">result of shot</param>
        public virtual void GetReportOfMyShot(Square square, SquareStatus status)
        {
            // if i did not shot
            if (!myTurn.HasValue || !myTurn.Value)
                throw new AggregateException("Can not receive report of my shot now");

            if (IsGameEnded)
                throw new AggregateException("Game ended");

            myTurn = status != SquareStatus.Miss;
            EnemyField.Shot(square, status, myId);
        }

        /// <summary>
        /// Return result of enemy's shot
        /// </summary>
        /// <param name="square">square of shot</param>
        /// <returns></returns>
        public SquareStatus ReportEnemyShotResult(Square square)
        {
            // if i must shot
            if (!myTurn.HasValue || myTurn.Value)
                throw new AggregateException("Can't receive shot now");

            if (IsGameEnded)
                throw new AggregateException("Game ended");

            var status = MyField.Shot(square, myId);
            myTurn = status == SquareStatus.Miss;
            return status;
        }
    }
}
