using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip.BusinessLogic
{
    public abstract class RealPlayer : Player
    {
        protected IPlayerInterface UI;
        protected IEnemyConnection EnemyConnection;

        protected RealPlayer(Field field, IEnemyConnection enemyConnection)
            : base(field)
        {
            if (enemyConnection == null)
                throw new ArgumentNullException(nameof(enemyConnection));
            EnemyConnection = enemyConnection;
            //UI.InterfaceClose += (sender, args) => { throw new NotImplementedException(); };
            new GameWindow().Show();
            fdfs();
        }

        public async void fdfs()
        {
            await Task.Delay(1000);
            MessageBox.Show("fdsf");
        }



        public override void EnemyDisconnected(bool active)
        {
            base.EnemyDisconnected(active); // Отобразить на форме
        }

        protected override void EndGame(bool win)
        {
            base.EndGame(win); // Отобразить на форме
        }

        protected sealed override void MarkSquareWithStatus(Square square, SquareStatus status, bool myField)
        {
            base.MarkSquareWithStatus(square, status, myField);
            UI.MarkSquareWithStatus(square, status, myField);
        }

    }

}
