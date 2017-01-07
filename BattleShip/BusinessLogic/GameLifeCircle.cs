﻿using System;
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
        public static void Start(MyBattleField myField, IGameUserInterface UI, IEnemyConnection enemy)
        {
            RealPlayer me = new RealPlayer(myField, enemy, UI);
            // provide info about first shot
            me.MyTurnInitialized += (sender, b) => 
                UI.ShowInfo(b? "You shoot first" : "Enemy shoot first", !b);

            // provide info about shots
            me.MyShot += (sender, args) => UI.ShowInfo($"My shot to {args.Square}: {args.SquareStatus}", !me.MyTurn);
            me.EnemyShot += (sender, args) => UI.ShowInfo($"Enemy's shot to {args.Square}: {args.SquareStatus}", !me.MyTurn);

            // change squarestatus in form
            me.MyField.SquareStatusChanged +=
                (sender, args) => UI.MarkMySquareWithStatus(args.Square, args.SquareStatus);
            me.EnemyField.SquareStatusChanged +=
                (sender, args) => UI.MarkEnemySquareWithStatus(args.Square, args.SquareStatus);

            // provide enfo about game end
            me.GameEnd += (sender, b) =>
            {
                UI.ShowGameEnd(b);
                var x = me.MyField.GetFullSquares();
                if (x.Any())
                    enemy.ShareEnemyMyFullSqures(x);
            };

            // show enemy full square on UI
            enemy.EnemySharedFullSquares += (sender, squares) => UI.ShowEnemyFullSquares(squares);
            enemy.CorruptedPacketReceived += (sender, args) =>  MessageBox.Show("Corrupted");
            // catch info from form
            UI.InterfaceForceClose += (sender, args) =>
            {
                me.ForceEndGame(false);
                enemy.Disconnect();
            };

            UI.GiveUp += (sender, args) =>
            {
                me.ForceEndGame(false);
                enemy.GiveUp();
                enemy.ShareEnemyMyFullSqures(me.MyField.GetFullSquares());
            };

            // start game
            ThreadPool.QueueUserWorkItem(obj => me.Start());
            UI.Start(me.MyField.GetFullSquares());
        }
    }
}
