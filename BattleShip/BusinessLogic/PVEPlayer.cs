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
    public class PVEPlayer : RealPlayer
    {
        public PVEPlayer(ClearField clearField, IEnemyConnection enemyConnection, IPlayerInterface userInterface) 
            : base(clearField, enemyConnection, userInterface) { }

        protected sealed override bool DecideWhoShotFirst()
        { return new Random().Next(2) == 1; }

    }
}
