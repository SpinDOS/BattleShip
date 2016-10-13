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
    sealed class GameLifeCircle
    {
        CreatingWindow createWindow = new CreatingWindow();

        public GameLifeCircle()
        {
            createWindow.StartGameEvent += StartGame;
        }

        public void Start()
        {
            createWindow.ShowDialog();
        }


        private void StartGame(object sender, StartGameEventArgs e)
        {
            createWindow.Hide();
            var enemyConnection = new MyRandomPlayerSimulator().GetConnectionToMe();
            var window = new GameWindow();
            var p = new PVEPlayer(e.ClearField, enemyConnection, window);
            p.Start();
            createWindow.Close();
        }
    }
}
