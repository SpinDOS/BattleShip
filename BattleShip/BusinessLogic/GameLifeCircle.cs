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
        /// Start game vs computer
        /// </summary>
        public static void StartPVE(MyBattleField myField, IGameUserInterface GameUI, IEnemyConnection enemyConnection)
        {
            // configure realPlayer, UI and connection to provide info
            var me = ConfigureRealPlayer(myField, GameUI, enemyConnection);

            // close connection on UI close
            GameUI.InterfaceForceClose += (sender, args) => enemyConnection.Disconnect();

            // start game
            ThreadPool.QueueUserWorkItem(obj => me.Start());
            GameUI.Start(me.MyField.GetFullSquares());
        }

        public static void StartPVP(MyBattleField myField, IGameUserPvpInterface pvpInterface,
            IEnemyConnection enemyConnection)
        {
            var me = ConfigureRealPlayer(myField, pvpInterface, enemyConnection);
            enemyConnection.CorruptedPacketReceived += (sender, args) => MessageBox.Show("Corrupted");
            enemyConnection.EnemyDisconnected += (sender, reason) =>
            {
                // me
                pvpInterface.ShowEnemyDisconnected(reason);
                
            };
            pvpInterface.InterfaceForceClose += (sender, args) =>
            {
                if (enemyConnection.IsConnected && pvpInterface.AskIfKeepConnection())
                    return;
                enemyConnection.Disconnect();
            };


        }

        public static void StartPVPWithCommunication(MyBattleField myField, IGameUserPvpInterface pvpInterface,
            IEnemyConnection enemyConnection, ICommunicationUserInterface communicationUserInterface,
            ICommunicationConnection communicationConnection)
        {
            communicationConnection.MessageReceived +=
                (sender, reader) => communicationUserInterface.ShowMessage(reader);
            communicationUserInterface.UserSentMessage +=
                (sender, writer) => communicationConnection.SendMessage(writer);
            StartPVP(myField, pvpInterface, enemyConnection);
        }

        private static RealPlayer ConfigureRealPlayer(MyBattleField myField, IGameUserInterface GameUI, IEnemyConnection enemyConnection)
        {
            RealPlayer me = new RealPlayer(myField, enemyConnection, GameUI);
            // provide info about first shot
            me.MyTurnInitialized += (sender, b) =>
                GameUI.ShowInfo(b ? "You shoot first" : "Enemy shoot first", !b);

            // provide info about shots
            me.MyShot += (sender, args) => GameUI.ShowInfo($"My shot to {args.Square}: {args.SquareStatus}", !me.MyTurn);
            me.EnemyShot += (sender, args) => GameUI.ShowInfo($"Enemy's shot to {args.Square}: {args.SquareStatus}", !me.MyTurn);

            // change squarestatus in form
            me.MyField.SquareStatusChanged +=
                (sender, args) => GameUI.MarkMySquareWithStatus(args.Square, args.SquareStatus);
            me.EnemyField.SquareStatusChanged +=
                (sender, args) => GameUI.MarkEnemySquareWithStatus(args.Square, args.SquareStatus);

            // provide enfo about game end
            me.GameEnd += (sender, b) =>
            {
                // send my full squares is connected
                var fullSquares = me.MyField.GetFullSquares();
                if (fullSquares.Any() && enemyConnection.IsConnected)
                    enemyConnection.SendEnemyMyFullSqures(fullSquares);

                // provide info about end game
                GameUI.ShowGameEnd(b);

            };

            // show enemy full square on UI
            enemyConnection.EnemySharedFullSquares += (sender, squares) => GameUI.ShowEnemyFullSquares(squares);

            // catch info from form
            GameUI.InterfaceForceClose += (sender, args) =>
            {
                me.ForceEndGame(false);
            };

            GameUI.GiveUp += (sender, args) =>
            {
                me.ForceEndGame(false);
                enemyConnection.GiveUp();
                enemyConnection.SendEnemyMyFullSqures(me.MyField.GetFullSquares());
            };
            return me;
        }

    }
}
