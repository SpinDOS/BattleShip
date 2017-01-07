using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    public interface IEnemyConnection : IGameConnection
    {
        /// <summary>
        /// True, if connected to enemy
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Report enemy positions of my full squares 
        /// </summary>
        /// <param name="fullSquares">collection of full squres</param>
        void ShareEnemyMyFullSqures(IEnumerable<Square> fullSquares);

        /// <summary>
        /// Raise when enemy reports its full squares
        /// </summary>
        event EventHandler<IEnumerable<Square>> EnemySharedFullSquares;

        /// <summary>
        /// Give up
        /// </summary>
        void GiveUp();

        /// <summary>
        /// Enemy gave up
        /// </summary>
        event EventHandler EnemyGaveUp;

        /// <summary>
        /// Disconnect
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Enemy disconnected
        /// </summary>
        event EventHandler<BattleShipDisconnectReason> EnemyDisconnected;
    }
}
