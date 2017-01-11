using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using BattleShip.BusinessLogic;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip
{
    /// <summary>
    /// Contain logic for coordinating forms and other objects
    /// </summary>
    public sealed class AppLifeCircle
    {
        // window to configure your field
        // game starts on event of CreatingWindow
        readonly CreatingWindow CreatingWindow = new CreatingWindow();

        // lazy initialized singleton ConnectingWindow
        private ConnectingWindow _connectingWindow = null;
        private ConnectingWindow ConnectingWindow => 
            _connectingWindow ?? (_connectingWindow = new ConnectingWindow() {Owner = CreatingWindow});

        public AppLifeCircle()
        {
            // handle starting game
            CreatingWindow.StartGameEvent += StartGame;
        }

        /// <summary>
        /// Start application life circle
        /// </summary>
        public void Start() => CreatingWindow.ShowDialog();

        // handler for start game click in creating form
        private void StartGame(object sender, StartGameEventArgs e)
        {
            // single template for creating game window
            Func<bool, GameWindow> createGameWindow = chat =>
            {
                // create window and center it 
                var window = new GameWindow {Owner = CreatingWindow};
                // show or hide chat block
                window.ShowChat(chat);
                return window;
            };

            // action to call to start game
            Action startGame = null;

            // if pvp
            if (e.VsHuman)
            {
                // try find enemy
                var clientAndListener = ConnectingWindow.Start();
                // if did not fid enemy
                if (clientAndListener == null)
                    return;
                // create RealConnection
                var connection = new RealConnection(clientAndListener);
                // create game window with chat
                var window = createGameWindow.Invoke(true);
                // start game as pvp mode with game window with chat
                startGame = () =>
                {
                    new GameLifeCircle(e.MyField, window, connection).StartPVPWithCommunication(window, connection);
                    // if after game enemy is disconnected - notify ConnectingWindow
                    if (!connection.IsConnected)
                        ConnectingWindow.ResetConnection();
                };
            }
            else // vs computer
            {
                SimulatedPlayer simEnemy;
                try
                { // ask difficulty
                    simEnemy = AskDifficulty(e.MyField);
                }
                catch (OperationCanceledException)
                { // is cancelled - do nothing
                    return;
                }
                // create connection
                var connection = new SimulatedConnection(simEnemy);
                var window = createGameWindow.Invoke(false);
                // start game as pve mode with game window without chat
                startGame = () => new GameLifeCircle(e.MyField, window, connection).StartPVE();
            }

            // hide creating window
            CreatingWindow.Hide();
            // start game
            startGame.Invoke();
            CreatingWindow.ShowDialog();
        }

        // create form and ask user for difficulty level
        // throw on cancellation
        private SimulatedPlayer AskDifficulty(MyBattleField myField)
        {
            var difChoose = new DifficultyChoose();
            // center window on CreatingWindow
            difChoose.Owner = CreatingWindow;
            int dif = difChoose.AskDifficulty();

            switch (dif)
            {
                case 1:
                    return new SimplePlayerSimulator();
                case 2:
                    return new MyRandomPlayerSimulator();
                case 3:
                    return new LogicalPlayerSimulator();
                case 4:
                    return new SmartPlayerSimulator();
                case 5:
                    return new CheaterPlayerSimulator(myField.GetFullSquares());
                default:
                    throw new AggregateException("Unknown difficulty level");
            }
        }
    }
}
