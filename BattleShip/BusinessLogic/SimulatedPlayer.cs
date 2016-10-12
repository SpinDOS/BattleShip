using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class SimulatedPlayer : Player
    {
        private readonly ConnectionToMe connectionToMe = null;

        protected SimulatedPlayer(Field field) : base(field)
        {connectionToMe = new ConnectionToMe(this); }

        public IEnemyConnection GetConnectionToMe() => connectionToMe;

        //public void Start()
        //{
        //    if (IsGameEnded)
        //        throw GameEndedException;
        //    if (MyTurn == null)
        //        throw NotInitializedException;
        //    while (!IsGameEnded)
        //    {
        //        if (MyTurn.Value)
        //        {
        //            Square square = GetMyNextShot();
        //            SquareStatus status = EnemyConnection.ShotEnemy(square);
        //            SetStatusOfMyShot(square, status);
        //        }
        //        else
        //        {
        //            Square square = EnemyConnection.GetShotFromEnemy();
        //            SquareStatus status = this.ShotFromEnemy(square);
        //            EnemyConnection.SendStatusOfEnemysShot(square, status);
        //        }
        //    }
        //}

        private class ConnectionToMe : IEnemyConnection
        {
            private SimulatedPlayer me;

            public ConnectionToMe(SimulatedPlayer player)
            {
                if (player == null)
                    throw new NullReferenceException(nameof(player));
                me = player;
            }

            public void SetEnemyShotFirst(bool enemyFirst)
            { me.SetMeShotFirst(enemyFirst);}

            public Square GetShotFromEnemy()
            { return me.GetMyNextShot(); }

            public void SendStatusOfEnemysShot(Square square, SquareStatus result)
            { me.SetStatusOfMyShot(square, result);}

            public SquareStatus ShotEnemy(Square square)
            { return me.ShotFromEnemy(square); }
        }

    }
}
