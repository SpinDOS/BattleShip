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
        // game starts on event of _createWindow
        readonly CreatingWindow _createWindow = new CreatingWindow();

        // connection to enemy. Can be saved between games 
        private RealConnection _realConnection = null;

        public AppLifeCircle()
        {
            // handle starting game
            _createWindow.StartGameEvent += StartGame;
        }

        /// <summary>
        /// Start application life circle
        /// </summary>
        public void Start() => _createWindow.ShowDialog();

        // handler for start game click in creating form
        private void StartGame(object sender, StartGameEventArgs e)
        {
            // single template for creating game window
            Func<bool, GameWindow> createGameWindow = chat =>
            {
                // create window and center it 
                var window = new GameWindow {Owner = _createWindow};
                // show or hide chat block
                window.ShowChat(chat);
                return window;
            };

            // action to call to start game
            Action startGame = null;

            // if pvp
            if (e.VsHuman)
            {
                // create game window with chat
                var window = createGameWindow.Invoke(true);

                // if connection were saved but enemy disconnected
                if (_realConnection != null && !_realConnection.IsConnected)
                {
                    _realConnection = null;
                    // todo window.showenemydisconnected
                }
                // if there is no alive connection
                if (_realConnection == null)
                {
                    // try establish connetion
                    var clientAndListener = new ConnectingWindow() {Owner = _createWindow}.Start();

                    // if failed to establish connection - do nothing
                    if (clientAndListener == null)
                        return;

                    // create realConnection on the just created connection
                    _realConnection = new RealConnection(clientAndListener);
                }
                // start game as pvp mode with game window with chat
                startGame = () => GameLifeCircle.StartPVPWithCommunication(e.MyField, window, _realConnection, window, _realConnection);
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
                // start game as pve mode with game window without chat
                startGame = () => GameLifeCircle.StartPVE(e.MyField, createGameWindow.Invoke(false), connection);
            }

            // hide creating window
            _createWindow.Hide();
            // start game
            startGame.Invoke();
            // if connection is alive, save it
            // else - forget
            if (!_realConnection.IsConnected)
                _realConnection = null;
            // show _creatingWindow again
            _createWindow.ShowDialog();
        }

        // create form and ask user for difficulty level
        // throw on cancellation
        private SimulatedPlayer AskDifficulty(MyBattleField myField)
        {
            var difChoose = new DifficultyChoose();
            // center window on _createWindow
            difChoose.Owner = _createWindow;
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
