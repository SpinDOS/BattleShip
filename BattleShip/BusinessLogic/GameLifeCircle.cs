using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Contain logic for handling single game
    /// </summary>
    public sealed class GameLifeCircle
    {
        /// <summary>
        /// Start game
        /// </summary>
        public static void Start(MyBattleField myField, GameWindow window, IEnemyConnection enemy)
        {
            RealPlayer me = new RealPlayer(myField, enemy, window);
            // provide info about first shot
            me.MyTurnInitialized += (sender, b) => 
                window.ShowInfo(b? "You shoot first" : "Enemy shoot first", !b);

            // provide info about shots
            me.MyShot += (sender, args) => window.ShowInfo($"My shot to {args.Square}: {args.SquareStatus}", !me.MyTurn);
            me.EnemyShot += (sender, args) => window.ShowInfo($"Enemy's shot to {args.Square}: {args.SquareStatus}", !me.MyTurn);

            // change squarestatus in form
            me.MyField.SquareStatusChanged +=
                (sender, args) => window.MarkMySquareWithStatus(args.Square, args.SquareStatus);
            me.EnemyField.SquareStatusChanged +=
                (sender, args) => window.MarkEnemySquareWithStatus(args.Square, args.SquareStatus);

            // provide enfo about game end
            me.GameEnd += (sender, b) =>
            {
                window.ShowGameEnd(b);
                if (!b)
                    window.ShowEnemyFullSquares(enemy.GetEnemyFullSquares());
            };

            // catch info from form
            window.InterfaceClose += (sender, args) => me.ForceEndGame(false);
            window.GiveUp += (sender, args) => me.ForceEndGame(false);

            // start game
            ThreadPool.QueueUserWorkItem(obj => me.Start());
            window.Start(me.MyField.GetFullSquares());
        }
    }
}
