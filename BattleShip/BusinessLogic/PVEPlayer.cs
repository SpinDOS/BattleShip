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
        public PVEPlayer(Field field, IEnemyConnection enemyConnection, IPlayerInterface userInterface) 
            : base(field, enemyConnection, userInterface) { }

        protected sealed override bool DecideWhoShotFirst()
        { return new Random().Next(2) == 1; }

        protected override Square GenerateNextShot()
        { return UI.GetMyShot(); }
    }
}
