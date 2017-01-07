using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        readonly CreatingWindow _createWindow = new CreatingWindow();
        private RealConnection _realConnection = null;

        public AppLifeCircle()
        {
            _createWindow.StartGameEvent += StartGame;
        }

        public void Start() => _createWindow.ShowDialog();

        // handler for start game click in creating form
        private void StartGame(object sender, StartGameEventArgs e)
        {
            Func<GameWindow> createGameWindow = () => new GameWindow() {Owner = _createWindow};
            Action startGame = null;
            if (!e.VsHuman)
            {
                SimulatedPlayer simEnemy;
                try
                { simEnemy = AskDifficulty(e.MyField); }
                catch (OperationCanceledException)
                { return; }
                var connection = new SimulatedConnection(simEnemy);
                startGame = () => GameLifeCircle.Start(e.MyField, createGameWindow.Invoke(), connection);
            }
            else
            {
                if (_realConnection == null || _realConnection.IsConnected)
                {
                    var clientAndListener = new ConnectingWindow() {Owner = _createWindow}.Start();
                    if (clientAndListener == null)
                        return;
                    _realConnection = new RealConnection(clientAndListener);
                }
                startGame = () => GameLifeCircle.Start(e.MyField, createGameWindow.Invoke(), _realConnection);
            }

            _createWindow.Hide();
            startGame.Invoke();
            _createWindow.ShowDialog();
        }

        // create form and ask user for difficulty level
        private SimulatedPlayer AskDifficulty(MyBattleField myField)
        {
            var difChoose = new DifficultyChoose();
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
                    throw new NotImplementedException("Unknown difficulty level");
            }
        }
    }
}
