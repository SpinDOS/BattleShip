using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    public interface IEnemyConnection
    {
        Square GetShotFromEnemy();
        void SendStatusOfEnemysShot(Square square, SquareStatus result);
        SquareStatus ShotEnemy(Square square);
        void Disconnect(bool active);
    }
}
