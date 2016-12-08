//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using BattleShip.DataLogic;
//using BattleShip.Shared;
//using BattleShip.UserLogic;

//namespace BattleShip.BusinessLogic
//{
//    public abstract class sRealPlayer : Player
//    {
//        protected readonly IPlayerInterface UI;
//        protected readonly IEnemyConnection EnemyConnection;

//        protected sRealPlayer(BattlefieldExtensions clearField, 
//            IEnemyConnection enemyConnection, IPlayerInterface playerInterface)
//            : base(clearField)
//        {
//            if (enemyConnection == null)
//                throw new ArgumentNullException(nameof(enemyConnection));
//            if (playerInterface == null)
//                throw new ArgumentNullException(nameof(playerInterface));
//            UI = playerInterface;
//            EnemyConnection = enemyConnection;
//            this.MySquareStatusChanged += (sender, args) => UI.MarkMySquareWithStatus(args.Square, args.SquareStatus);
//            this.EnemySquareStatusChanged += (sender, args) => UI.MarkEnemySquareWithStatus(args.Square, args.SquareStatus);
//            this.EnemysShot += (sender, args) =>
//            {
//                UI.ShowInfo("Sending answer to enemy...", true);
//                EnemyConnection.SendStatusOfEnemysShot(args.Square, args.SquareStatus);
//            };
//            this.GameEnded += ShowEndGameInUi;
//            UI.InterfaceClose += (sender, args) => { Exit(); };
//        }

//        protected abstract bool DecideWhoShotFirst();

//        protected sealed override Square GenerateNextShot()
//        {
//            return UI.GetMyShot();
//        }

//        public void Start()
//        {
//            if (IsGameEnded)
//                throw new AggregateException("Create new player to start again");

//            ThreadPool.QueueUserWorkItem(o =>
//            {
//                UI.ShowInfo("Deciding who shot first...", true);
//                bool meFirst = DecideWhoShotFirst();
//                SetMeShotFirst(meFirst);
//                if (meFirst)
//                    UI.ShowInfo("You shot first", false);
//                else
//                    UI.ShowInfo("Enemy shots first", true);
//                while (!IsGameEnded)
//                {
//                    try
//                    {
//                        if (MyTurn.Value)
//                        {
//                            Square square = GetMyNextShot();
//                            UI.ShowInfo("Waiting for result of your shot...", true);
//                            SquareStatus status = EnemyConnection.ShotEnemy(square);
//                            UI.ShowInfo("Deciding what to do next...", true);
//                            SetStatusOfMyShot(square, status);
//                            if (IsGameEnded)
//                                continue;
//                            if (status == SquareStatus.Miss)
//                                UI.ShowInfo("You miss! Enemy's turn to shot", true);
//                            else
//                                UI.ShowInfo("Nice! You shot again", false);
//                        }
//                        else
//                        {
//                            Square square = EnemyConnection.GetShotFromEnemy();
//                            UI.ShowInfo("Deciding what to do next...", true);
//                            SquareStatus status = this.ShotFromEnemy(square);
//                            if (IsGameEnded)
//                                continue;
//                            if (status == SquareStatus.Miss)
//                                UI.ShowInfo("Enemy misses! Your turn to shot", false);
//                            else
//                                UI.ShowInfo("Oh! Enemy shots again", true);
//                        }
//                    }
//                    catch (GameEndedException)
//                    {
//                        if (!IsGameEnded)
//                            EndGame(true);
//                        break;
//                    }
//                }
//            });

//            UI.Start(BattlefieldExtensions.Validate(MyField.GetFullSquares()));
//        }

//        protected void Exit()
//        {
//            if (IsGameEnded)
//                return;
//            this.GameEnded -= ShowEndGameInUi;
//            this.EndGame(false);
//            EnemyConnection.Disconnect();
//        }

//        private void ShowEndGameInUi(object sender, bool win)
//        {
//            UI.ShowGameEnd(win);
//            if (win)
//                return;
//            UI.ShowInfo("You lost. Getting enemy's full squares", true);
//            var enemySquares = EnemyConnection.GetEnemyFullSquares().ToArray();
//            foreach (var square in enemySquares)
//                UI.MarkEnemySquareWithStatus(square, SquareStatus.Full);
//            UI.ShowInfo("You lost!(", true);
//        }
//    }

//}
