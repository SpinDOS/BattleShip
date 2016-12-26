using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleShip.BusinessLogic;
using BattleShip.DataLogic;
using BattleShip.UserLogic;

namespace BattleShip
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new CreatingWindow().Show();
            try
            {
                new ConnectionEstablisher().CreateLobby();
                MessageBox.Show("");
            }
            catch { }
            //new AppLifeCircle().Start();
        }
    }
}
