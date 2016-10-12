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
            var window = new GameWindow();
            var p = new PVEPlayer(e.Field, new MyRandomPlayerSimulator().GetConnectionToMe(), window);
            //window.ShowDialog();
            createWindow.Close();
        }
    }
}
