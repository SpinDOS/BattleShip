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
        private IGameConnection enemy;
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
        /// Trigger when MyTurn initialized
        /// </summary>
        public event EventHandler<bool> MyTurnInitialized; 

        /// <summary>
        /// Trigger when game ends
        /// </summary>
        public event EventHandler<bool> GameEnd;

        public bool MyTurn { get; private set; } = false;

        public RealPlayer(MyBattleField myField, 
            IGameConnection enemy, IMyShotSource me) : base(myField)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));
            if (me == null)
                throw new ArgumentNullException(nameof(me));
            this.enemy = enemy;
            this.me = me;
            
            // events initialization
            MyShot += (sender, args) => EnemyField.Shot(args.Square, args.SquareStatus, myId);
            GameEnd += (sender, b) => MyTurn = false;
        }

        public void Start()
        {
            // decide who first
            MyTurn = enemy.IsMeShootFirst();
            MyTurnInitialized?.Invoke(this, MyTurn);
            while (!IsGameEnded)
            {
                Square square;
                SquareStatus status;
                if (MyTurn)
                {
                    // shot
                    square = me.GetMyShot();
                    status = enemy.ShotEnemy(square);

                    // i shot again if i didnot miss
                    MyTurn = status != SquareStatus.Miss;

                    // call event
                    MyShot?.Invoke(this, new ShotEventArgs(square, status)); // mark in field

                    // check for end game
                    if (EnemyField.ShipsAlive == 0)
                    {
                        IsGameEnded = true;
                        GameEnd?.Invoke(this, true);
                    }

                    
                }
                else
                {
                    // shot
                    square = enemy.GetShotFromEnemy();
                    status = MyField.Shot(square, myId); // mark in field

                    // report enemy
                    enemy.SendStatusOfEnemysShot(square, status);

                    MyTurn = status == SquareStatus.Miss;

                    // call event
                    EnemyShot?.Invoke(this, new ShotEventArgs(square, status));

                    // check for end game
                    if (MyField.ShipsAlive == 0)
                    {
                        IsGameEnded = true;
                        GameEnd?.Invoke(this, false);
                    }
                    
                }

            }
        }

        /// <summary>
        /// Force end game if someone gave up
        /// </summary>
        public override void ForceEndGame(bool win)
        {
            base.ForceEndGame(win);
            GameEnd?.Invoke(this, win);
        }
    }
}
