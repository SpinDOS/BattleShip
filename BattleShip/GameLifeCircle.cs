using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.BusinessLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip
{
    class GameLifeCircle
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
            //Player player = 
            createWindow.Hide();
            throw new NotImplementedException();
        }
    }
}
