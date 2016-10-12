using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip.BusinessLogic
{
    public abstract class RealPlayer : Player
    {
        protected IPlayerInterface UI;
        protected IEnemyConnection EnemyConnection;

        protected RealPlayer(Field field, IEnemyConnection enemyConnection, IPlayerInterface playerInterface)
            : base(field)
        {
            if (enemyConnection == null)
                throw new ArgumentNullException(nameof(enemyConnection));
            if (playerInterface == null)
                throw new ArgumentNullException(nameof(playerInterface));
            UI = playerInterface;
            EnemyConnection = enemyConnection;
            //UI.InterfaceClose += (sender, args) => { throw new NotImplementedException(); };
        }

        protected abstract bool DecideWhoShotFirst();

        public void Start()
        {
            bool meFirst = DecideWhoShotFirst();
            SetMeShotFirst(meFirst);
            EnemyConnection.SetEnemyShotFirst(!meFirst);
            while (!IsGameEnded)
            {
                if (MyTurn.Value)
                {
                    Square square = GetMyNextShot();
                    SquareStatus status = EnemyConnection.ShotEnemy(square);
                    SetStatusOfMyShot(square, status);
                }
                else
                {
                    Square square = EnemyConnection.GetShotFromEnemy();
                    SquareStatus status = this.ShotFromEnemy(square);
                    EnemyConnection.SendStatusOfEnemysShot(square, status);
                }
            }
        }
    }

}
