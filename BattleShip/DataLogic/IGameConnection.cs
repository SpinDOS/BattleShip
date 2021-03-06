﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Connection with enemy only for internal game needs
    /// </summary>
    public interface IGameConnection : IDisposable
    {
        /// <summary>
        /// Detect who shoot first
        /// </summary>
        bool IsMeShootFirst();
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
    }
}
