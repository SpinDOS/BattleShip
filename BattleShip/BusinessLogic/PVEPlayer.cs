using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip.BusinessLogic
{
    class PVEPlayer : RealPlayer
    {
        public PVEPlayer(Field field, IEnemyConnection enemyConnection, IPlayerInterface userInterface) 
            : base(field, enemyConnection, userInterface) { }

        public override void EnemyDisconnected(bool active)
        {
            throw new AggregateException("Computer can't disconnect");
        }
    }
}
