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
        CreatingWindow createWindow = new CreatingWindow();

        public AppLifeCircle()
        {
            createWindow.StartGameEvent += StartGame;
        }

        public void Start() => createWindow.ShowDialog();

        // handler for start game click in creating form
        private void StartGame(object sender, StartGameEventArgs e)
        {
            SimulatedPlayer sim_enemy = null;
            if (!e.VsHuman)
            {
                try
                { sim_enemy = AskDifficulty(e.MyField); }
                catch (OperationCanceledException)
                { return; }
            }

            createWindow.Hide();
            var enemyConnection = new SimulatedConnection(sim_enemy);
            GameLifeCircle.Start(e.MyField, new GameWindow(), enemyConnection);
            createWindow.ShowDialog();
        }

        // create form and ask user for difficulty level
        private SimulatedPlayer AskDifficulty(MyBattleField myField)
        {
            var difChoose = new DifficultyChoose();
            difChoose.Owner = createWindow;
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
