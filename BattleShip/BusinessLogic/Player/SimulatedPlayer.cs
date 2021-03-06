﻿using System;
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
                throw new GameStateException("Player is already initialized");
            if (IsGameEnded)
                throw new GameStateException("Game ended");
            myTurn = meFirst;
        }

        /// <summary>
        /// Set result of my shot
        /// </summary>
        /// <param name="square">target of shot</param>
        /// <param name="status">result of shot</param>
        public virtual void GetReportOfMyShot(Square square, SquareStatus status)
        {
            if (IsGameEnded)
                throw new GameStateException("Game ended");

            // if i did not shot
            if (!myTurn.HasValue || !myTurn.Value)
                throw new GameStateException("Can not receive report of my shot now");
            
            EnemyField.Shot(square, status, myId);
            if (EnemyField.ShipsAlive == 0)
                IsGameEnded = true;

            myTurn = status != SquareStatus.Miss;
        }

        /// <summary>
        /// Return result of enemy's shot
        /// </summary>
        /// <param name="square">square of shot</param>
        /// <returns></returns>
        public SquareStatus ReportEnemyShotResult(Square square)
        {
            if (IsGameEnded)
                throw new GameStateException("Game ended");

            // if i must shot
            if (!myTurn.HasValue || myTurn.Value)
                throw new GameStateException("Can't receive shot now");

            var status = MyField.Shot(square, myId);
            if (MyField.ShipsAlive == 0)
                IsGameEnded = true;

            myTurn = status == SquareStatus.Miss;
            return status;
        }

        /// <summary>
        /// End game if someone gave up
        /// </summary>
        public sealed override void ForceEndGame(bool win) => base.ForceEndGame(win);
        // prevent overriding

    }
}
