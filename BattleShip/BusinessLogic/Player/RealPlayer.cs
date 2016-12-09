using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Class for logic of real player
    /// </summary>
    public sealed class RealPlayer : Player
    {
        // connection with enemy
        private IEnemyConnection enemy;
        // source of my next shot
        private IMyShotSource me;

        /// <summary>
        /// Trigger when i get result of my shot
        /// </summary>
        public event EventHandler<ShotEventArgs> MyShot;
        /// <summary>
        /// Trigger when enemy shot me
        /// </summary>
        public event EventHandler<ShotEventArgs> EnemyShot;

        /// <summary>
        /// Trigger when game ends
        /// </summary>
        public event EventHandler<bool> GameEnd; 

        public RealPlayer(MyBattleField myField, IEnemyConnection enemy, IMyShotSource me) : base(myField)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));
            if (me == null)
                throw new ArgumentNullException(nameof(me));
            this.enemy = enemy;
            this.me = me;
            
            // events initialization
            MyShot += (sender, args) => EnemyField.Shot(args.Square, args.SquareStatus, myId);

        }

        public void Start(CancellationToken ct)
        {
            // decide who first
            bool myTurn = enemy.IsMeShotFirst();
            while (!ct.IsCancellationRequested && !IsGameEnded)
            {
                Square square;
                SquareStatus status;
                if (myTurn)
                {
                    // shot
                    square = me.GetMyShot();
                    status = enemy.ShotEnemy(square);

                    // call event
                    MyShot?.Invoke(this, new ShotEventArgs(square, status)); // mark in field

                    // check for end game
                    if (EnemyField.ShipsAlive == 0)
                    {
                        IsGameEnded = true;
                        GameEnd?.Invoke(this, true);
                    }

                    // i shot again if i didnot miss
                    myTurn = status != SquareStatus.Miss;
                }
                else
                {
                    // shot
                    square = enemy.GetShotFromEnemy();
                    status = MyField.Shot(square, myId); // mark in field

                    // report enemy
                    enemy.SendStatusOfEnemysShot(square, status);

                    // call event
                    EnemyShot?.Invoke(this, new ShotEventArgs(square, status));

                    // check for end game
                    if (MyField.ShipsAlive == 0)
                    {
                        IsGameEnded = true;
                        GameEnd?.Invoke(this, false);
                    }

                    myTurn = status == SquareStatus.Miss;
                }

            }
        }

        public override void ForceEndGame(bool win)
        {
            base.ForceEndGame(win);
            GameEnd?.Invoke(this, win);
        }
    }
}
