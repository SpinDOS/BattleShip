﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Connection with enemy only for game
    /// </summary>
    public interface IEnemyConnection : IDisposable
    {
        /// <summary>
        /// Detect who shot first
        /// </summary>
        bool IsMeShotFirst();
        /// <summary>
        /// Get shot from enemy
        /// </summary>
        Square GetShotFromEnemy();
        /// <summary>
        /// Report enemy result of its shot
        /// </summary>
        void SendStatusOfEnemysShot(Square square, SquareStatus result);
        /// <summary>
        /// Shot enemy
        /// </summary>
        SquareStatus ShotEnemy(Square square);
        /// <summary>
        /// Give up
        /// </summary>
        bool GiveUp();
        /// <summary>
        /// Get not hurt squares of enemy
        /// </summary>
        IEnumerable<Square> GetEnemyFullSquares();
    }
}
