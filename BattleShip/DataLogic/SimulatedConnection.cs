using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    class SimulatedConnection : IEnemyConnection
    {
        private SimulatedPlayer enemy;
        public bool IsMeShotFirst()
        {
            bool realFirst = new Random().Next(2) == 0;
            enemy.SetMeShotFirst(!realFirst);
            return realFirst;
        }

        public Square GetShotFromEnemy()
        {
            return enemy.GetNextShot();
        }

        public void SendStatusOfEnemysShot(Square square, SquareStatus result)
        {
            enemy.GetReportOfMyShot(square, result);
        }

        public SquareStatus ShotEnemy(Square square)
        {
            return enemy.ReportEnemyShotResult(square);
        }
    }
}
