using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Contain logic for handling single game
    /// </summary>
    public sealed class GameLifeCircle
    {
        // UnSubscribeConnection - for unsubsribe connection events to prevent handling event on closed form
        // UnSubscribeGameEnd - for unsubsribe gameend events to prevent displaying multiple notifications about game end
        private Action UnSubscribeConnection, UnSubscribeGameEnd;

        /// <summary>
        /// Create GameLifeCircle for starting game
        /// </summary>
        /// <param name="yourField">Field with your ship squares</param>
        /// <param name="gameUI">User interface to provide info and get your shots</param>
        /// <param name="enemyConnection">Connection to the enemy</param>
        public GameLifeCircle(MyBattleField yourField, IGameUserInterface gameUI, IEnemyConnection enemyConnection)
        {
            // save parameters to properties for next use
            GameUI = gameUI;
            EnemyConnection = enemyConnection;

            // create player
            RealPlayer = new RealPlayer(yourField, enemyConnection, gameUI);

            // REALPLAYER EVENTS
            //**********************************************************************************************************************************

            // provide info about first shot
            RealPlayer.MyTurnInitialized += (sender, b) =>
                gameUI.ShowInfo(b ? "You shoot first" : "Enemy shoot first", !b);

            // provide info about shots
            RealPlayer.MyShot += (sender, args) => 
                gameUI.ShowInfo($"My shot to {args.Square}: {args.SquareStatus}", !RealPlayer.MyTurn);
            RealPlayer.EnemyShot += (sender, args) => 
                gameUI.ShowInfo($"Enemy's shot to {args.Square}: {args.SquareStatus}", !RealPlayer.MyTurn);

            // change squarestatus in form
            RealPlayer.MyField.SquareStatusChanged += (sender, args) => gameUI.MarkMySquareWithStatus(args.Square, args.SquareStatus);
            RealPlayer.EnemyField.SquareStatusChanged += (sender, args) => gameUI.MarkEnemySquareWithStatus(args.Square, args.SquareStatus);

            // if enemy is connected, send him my full squares
            // this action check connection.isConnected, so there is no need to unsubscribe it
            RealPlayer.GameEnd += (sender, b) =>
            {
                // send my full squares is connected
                var fullSquares = RealPlayer.MyField.GetFullSquares();
                if (fullSquares.Any() && enemyConnection.IsConnected)
                    enemyConnection.SendEnemyMyFullSqures(fullSquares);
            };
            // provide enfo about game end
            EventHandler<bool> gameEnd = (sender, b) => gameUI.ShowGameEnd(b);
            RealPlayer.GameEnd += gameEnd;

            // CONNECTION EVENTS
            //**********************************************************************************************************************************

            // show enemy full square on UI
            EventHandler<IEnumerable<Square>> enemySharedFullSquares = (sender, squares) => gameUI.ShowEnemyFullSquares(squares);
            enemyConnection.EnemySharedFullSquares += enemySharedFullSquares;

            // USER INTERFACE EVENTS
            //**********************************************************************************************************************************

            // on interface close
            gameUI.InterfaceForceClose += (sender, args) =>
            {
                // not provide info to form and forget connection
                UnSubscribeConnection();
                // report realPlayer that you gave up
                if (!RealPlayer.IsGameEnded)
                    RealPlayer.ForceEndGame(false);
            };

            // when you want to give up
            EventHandler giveUp = (sender, args) =>
            {
                // not provide info about game end
                UnSubscribeGameEnd();
                // report realPlayer that you gave up
                RealPlayer.ForceEndGame(false);
                // send info to enemy if connected
                if (enemyConnection.IsConnected)
                {
                    enemyConnection.GiveUp();
                    enemyConnection.SendEnemyMyFullSqures(RealPlayer.MyField.GetFullSquares());
                }
            };
            gameUI.GiveUp += giveUp;


            // Unsubscribe section
            UnSubscribeConnection = () =>
            {
                // on every connection drop, info to form is provided by special method
                // no need to provide info about game end when connection is dropped
                UnSubscribeGameEnd();
                enemyConnection.EnemySharedFullSquares -= enemySharedFullSquares;
            };
            UnSubscribeGameEnd = () =>
            {
                // if game ended, forget about realPlayer.GameEnd and user give up
                RealPlayer.GameEnd -= gameEnd;
                gameUI.GiveUp -= giveUp;
            };
        }

        public RealPlayer RealPlayer { get; }
        public IGameUserInterface GameUI { get; }
        public IEnemyConnection EnemyConnection { get; }

        /// <summary>
        /// Start game vs computer
        /// </summary>
        public void StartPVE()
        {
            // close connection on UI close
            GameUI.InterfaceForceClose += (sender, args) => EnemyConnection.Disconnect();
            Start();
        }

        /// <summary>
        /// Start game vs another player on the internet
        /// </summary>
        public void StartPVP()
        {
            // check parameter
            var pvpInterface = GameUI as IGameUserPvpInterface;
            if (pvpInterface == null)
                throw new ArgumentException("GameUI can not be used for pvp game");

            // on enemy gave up
            EventHandler enemyGaveUp = (sender, args) =>
            {
                // prevent another notifications and notify RealPlayer and pvpInterface
                UnSubscribeGameEnd();
                RealPlayer.ForceEndGame(true);
                pvpInterface.ShowEnemyGaveUp();
            };
            EnemyConnection.EnemyGaveUp += enemyGaveUp;

            // on getting corrupted packet - notify user and disconnect
            EventHandler<DataEventArgs> corruptedPacketReceived = (sender, args) =>
            {
                // drop connection
                UnSubscribeConnection();
                EnemyConnection.Disconnect();
                RealPlayer.ForceEndGame(false);
                // provide info about error
                pvpInterface.ShowError("Some error occured in the connection to the opponent. " + 
                    "The connection will be dropped and the game is ended");
            };
            EnemyConnection.CorruptedPacketReceived += corruptedPacketReceived;

            // on enemy disconnected
            EventHandler<DisconnectReason> enemyDisconnected = (sender, reason) =>
            {
                // don't show new notification of game end and forget connection
                UnSubscribeConnection();
                // if disconnect is not made by you
                if (reason != DisconnectReason.DisconnectCalled)
                {
                    // you win
                    RealPlayer.ForceEndGame(true);
                    // provide info to UI
                    pvpInterface.ShowEnemyDisconnected(reason);
                }
                else
                {
                    // you give up
                    RealPlayer.ForceEndGame(false);
                }
            };
            EnemyConnection.EnemyDisconnected += enemyDisconnected;

            // on interface close - ask if keep connection
            pvpInterface.InterfaceForceClose += (sender, args) =>
            {
                // if connected - ask if user wants to drop connection or not
                if (EnemyConnection.IsConnected)
                {
                    // ask if to keep connection
                    var keepConnection = pvpInterface.AskIfKeepConnection();
                    // check again if enemy has disconnected while pvpInterface.AskIfKeepConnection()
                    if (!EnemyConnection.IsConnected)
                        return;
                    // if interface closes before game end
                    if (!RealPlayer.IsGameEnded)
                    {
                        // report enemy my full squares 
                        EnemyConnection.SendEnemyMyFullSqures(RealPlayer.MyField.GetFullSquares());
                    }
                    if (keepConnection)
                            EnemyConnection.GiveUp();
                    else // if want to disconnect
                        EnemyConnection.Disconnect();
                }
            };

            // update unSubscribe actions
            UnSubscribeGameEnd += () =>
            {
                EnemyConnection.EnemyGaveUp -= enemyGaveUp;
                EnemyConnection.CorruptedPacketReceived -= corruptedPacketReceived;
            };
            
            // other handlers are called in UnSubscribeGameEnd
            UnSubscribeConnection += () =>EnemyConnection.EnemyDisconnected -= enemyDisconnected;

            Start();
        }

        /// <summary>
        /// Start game vs another player on the internet with ability to communicate
        /// </summary>
        /// <param name="communicationUserInterface">UI to get and show messages</param>
        /// <param name="communicationConnection">Connection to receive and send messages</param>
        public void StartPVPWithCommunication(ICommunicationUserInterface communicationUserInterface,
            ICommunicationConnection communicationConnection)
        {
            // subscribe to send and receive messages
            EventHandler<NetDataReader> messageReceive = (sender, reader) => communicationUserInterface.ShowMessage(reader);
            EventHandler<NetDataWriter> sendMessage = (sender, writer) => communicationConnection.SendMessage(writer);
            communicationConnection.MessageReceived += messageReceive;
            communicationUserInterface.UserSentMessage += sendMessage;
            // Add these handlers to UnSubscribeConnection 
            UnSubscribeConnection += () =>
            {
                communicationConnection.MessageReceived -= messageReceive;
                communicationUserInterface.UserSentMessage -= sendMessage;
            };
            StartPVP();
        }

        // Start the game and show UI
        private void Start()
        {
            // start game and check exception if thrown
            Task.Run(() => RealPlayer.Start())
                .ContinueWith(t => // handle exceptions
                {   // if exception is not caused by disconnect or give up
                    if (!CheckException(t.Exception))
                    {
                        // end game and provide info to user
                        UnSubscribeGameEnd();
                        RealPlayer.ForceEndGame(false);
                        GameUI.ShowError("Some error occured during the game. The game is ended");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            GameUI.Start(RealPlayer.MyField.GetFullSquares());
        }

        // check exception if it contains any not ObjectDisposedException and not GiveUpException exceptions
        // true, if does not contain
        private bool CheckException(Exception exception)
        {
            var asAggregate = exception as AggregateException;
            // if the exception is not the task-exception, that contains another exceptions
            if (asAggregate == null || !asAggregate.InnerExceptions.Any())
                return exception is ObjectDisposedException || exception is GiveUpException;

            // check all inner exceptions
            foreach (var inner in asAggregate.InnerExceptions)
            {
                // if any bad - return false
                if (!CheckException(inner))
                    return false;
            }
            return true;
        }
    }
}

