using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;
using LiteNetLib;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Connection to the enemy for game control
    /// </summary>
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
        void SendEnemyMyFullSqures(IEnumerable<Square> fullSquares);

        /// <summary>
        /// Raise when enemy reports its full squares
        /// </summary>
        event EventHandler<IEnumerable<Square>> EnemySharedFullSquares;
        
        /// <summary>
        /// Raised when received corrupted packet 
        /// </summary>
         event EventHandler<DataEventArgs> CorruptedPacketReceived;

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
        event EventHandler<BattleShipConnectionDisconnectReason> EnemyDisconnected;
    }
}
