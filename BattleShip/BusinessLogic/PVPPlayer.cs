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
    public class PVPPlayer : RealPlayer
    {
        public PVPPlayer(ClearField clearField, IEnemyConnectionWithPeople enemyConnection,
            IPVPInterface userInterface)
            : base(clearField, enemyConnection, userInterface)
        {
            //this.GameEnded += 
        }

        protected sealed override bool DecideWhoShotFirst()
        {
            IEnemyConnectionWithPeople enemyCon = EnemyConnection as IEnemyConnectionWithPeople;
            Random rnd = new Random();
            bool me = true;
            while (true)
            {
                me = rnd.Next(2) == 0;
                enemyCon.SetEnemyShotFirst(!me);
                if (me == enemyCon.GetMeShotFirst())
                    break;
            }
            return me;
        }
    }
}
