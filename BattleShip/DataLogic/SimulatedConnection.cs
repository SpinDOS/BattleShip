using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Simulated connection to simulated player
    /// </summary>
    class SimulatedConnection : IEnemyConnection
    {
        private bool disposed = false;
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

        /// <summary>
        /// Detect who shot first
        /// </summary>
        public bool IsMeShotFirst()
        {
            if (disposed)
                throw new ObjectDisposedException("Connection is closed");
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
            if (disposed)
                throw new ObjectDisposedException("Connection is closed");
            return enemy.GetNextShot();
        }

        /// <summary>
        /// Report enemy result of its shot
        /// </summary>
        public void SendStatusOfEnemysShot(Square square, SquareStatus result)
        {
            if (disposed)
                throw new ObjectDisposedException("Connection is closed");
            enemy.GetReportOfMyShot(square, result);
        }

        /// <summary>
        /// Get result of my shot
        /// </summary>
        public SquareStatus ShotEnemy(Square square)
        {
            if (disposed)
                throw new ObjectDisposedException("Connection is closed");
            return enemy.ReportEnemyShotResult(square);
        }

        /// <summary>
        /// Give up
        /// </summary>
        public bool GiveUp()
        {
            if (disposed)
                throw new ObjectDisposedException("Connection is closed");
            enemy.ForceEndGame();
            return true;
        }

        /// <summary>
        /// Get not hurt squares of enemy's field
        /// </summary>
        public IEnumerable<Square> GetEnemyFullSquares()
        {
            if (disposed)
                throw new ObjectDisposedException("Connection is closed");
            return enemy.MyField.GetFullSquares();
        }

        public void Dispose()
        {
            if (!disposed)
                disposed = true;
        }
    }
}
