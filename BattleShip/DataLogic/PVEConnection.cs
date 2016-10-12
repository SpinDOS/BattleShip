using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    class PVEConnection : IEnemyConnection
    {
        private readonly SimulatedPlayer _playerSimulator = null;
        public PVEConnection(SimulatedPlayer playerSimulator)
        {
            if (playerSimulator == null)
                throw new ArgumentNullException(nameof(playerSimulator));
            _playerSimulator = playerSimulator;
        }
        public Square GetShotFromEnemy()
        {
            return _playerSimulator.GetMyNextShot();
        }

        public void SendStatusOfEnemysShot(Square square, SquareStatus result)
        {
            _playerSimulator.SetStatusOfMyShot(square, result);
        }
        public SquareStatus ShotEnemy(Square square)
        {
            return _playerSimulator.ShotFromEnemy(square);
        }

        public void Disconnect(bool active)
        {
            _playerSimulator.EnemyDisconnected(true);
        }
    }
}
