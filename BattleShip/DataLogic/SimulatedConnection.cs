using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.BusinessLogic;
using BattleShip.Shared;
using LiteNetLib;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Simulated connection to simulated player
    /// </summary>
    public sealed class SimulatedConnection : IEnemyConnection
    {
        // single exception for all ObjectDisposedException usages
        private readonly ObjectDisposedException _disposedException = 
            new ObjectDisposedException("Connection is closed");

        // enemy to play with
        private SimulatedPlayer enemy;

        /// <summary>
        /// Create connection with simulated player
        /// </summary>
        public SimulatedConnection(SimulatedPlayer enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));
            this.enemy = enemy;
        }

        #pragma warning disable

        /// <summary>
        /// Need to implement IEnemyConnection. This event is never raised
        /// </summary>
        public event EventHandler EnemyGaveUp;

        /// <summary>
        /// Need to implement IEnemyConnection. This event is never raised
        /// </summary>
        public event EventHandler<BattleShipConnectionDisconnectReason> EnemyDisconnected;

        /// <summary>
        /// Need to implement IEnemyConnection. This event is never raised
        /// </summary>
        public event EventHandler<DataEventArgs> CorruptedPacketReceived;

        #pragma warning restore

        /// <summary>
        /// Raise when enemy reports its full squares
        /// </summary>
        public event EventHandler<IEnumerable<Square>> EnemySharedFullSquares;

        /// <summary>
        /// True if connected to enemy
        /// </summary>
        public bool IsConnected { get; private set; } = true; // also indicate disposed state

        /// <summary>
        /// Detect who shot first
        /// </summary>
        public bool IsMeShootFirst()
        {
            if (!IsConnected)
                throw _disposedException;
            bool realFirst = new Random().Next(2) == 0;
            // report enemy
            enemy.SetMeShotFirst(!realFirst);
            return realFirst;
        }

        /// <summary>
        /// Get shot from enemy
        /// </summary>
        public Square GetShotFromEnemy()
        {
            if (!IsConnected)
                throw _disposedException;
            return enemy.GetNextShot();
        }

        /// <summary>
        /// Report enemy result of its shot
        /// </summary>
        public void SendStatusOfEnemysShot(Square square, SquareStatus result)
        {
            if (!IsConnected)
                throw _disposedException;
            enemy.GetReportOfMyShot(square, result);
            // report enemy full squres if needed
            if (enemy.IsGameEnded)
                EnemySharedFullSquares?.Invoke(this, enemy.MyField.GetFullSquares());
        }

        /// <summary>
        /// Get result of my shot
        /// </summary>
        public SquareStatus ShotEnemy(Square square)
        {
            if (!IsConnected)
                throw _disposedException;
            return enemy.ReportEnemyShotResult(square);
        }

        /// <summary>
        /// Do nothing because simulated player does not need your full squares
        /// </summary>
        public void SendEnemyMyFullSqures(IEnumerable<Square> fullSquares)
        {
            // do nothing
        }

        /// <summary>
        /// Give up
        /// </summary>
        public void GiveUp()
        {
            if (!IsConnected)
                throw _disposedException;
            // report enemy full squres
            EnemySharedFullSquares?.Invoke(this, enemy.MyField.GetFullSquares());
            // drop connection
            Disconnect();
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        public void Disconnect() => Dispose();

        /// <summary>
        /// Close the connection
        /// </summary>
        public void Dispose()
        {
            if (!IsConnected)
                return;

            IsConnected = false;
            // report enemy that he won
            if (!enemy.IsGameEnded)
                enemy.ForceEndGame(true);
            // free memore of enemy object
            enemy = null;
        }
    }
}
