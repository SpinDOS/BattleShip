using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.BusinessLogic;
using BattleShip.UserLogic;

namespace BattleShip
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new ConnectingWindow().ShowDialog();
            //new AppLifeCircle().Start();
        }
    }
}
