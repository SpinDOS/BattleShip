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
    public abstract class RealPlayer : Player
    {
        protected IPlayerInterface UI;
        protected IEnemyConnection EnemyConnection;

        protected RealPlayer(Field field, IEnemyConnection enemyConnection, IPlayerInterface playerInterface)
            : base(field)
        {
            UI = playerInterface;
            EnemyConnection = enemyConnection;
            playerInterface.Start(field);
            if (enemyConnection == null)
                throw new ArgumentNullException(nameof(enemyConnection));
        }

        public virtual void Start()
        {
            bool youshot = true;
            while (true)
            {
                if (youshot)
                {
                    Square square = GetMyNextShot();
                    SquareStatus status = EnemyConnection.ShotEnemy(square);
                    this.SetStatusOfMyShot(square, status);
                    youshot = status != SquareStatus.Miss;
                }
                else
                {
                    Square square = EnemyConnection.GetShotFromEnemy();
                    SquareStatus status = this.ShotFromEnemy(square);
                    EnemyConnection.SendStatusOfEnemysShot(square, status);
                    youshot = status == SquareStatus.Miss;
                }
            }
        }

        public override Square GetMyNextShot()
        {
            return UI.GetMyShot();
        }

        protected override void MarkSquareWithStatus(Square square, SquareStatus status, bool myField)
        {
            base.MarkSquareWithStatus(square, status, myField);
            UI.MarkSquareWithStatus(square, status, myField);
        }
    }

}
