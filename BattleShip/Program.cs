using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleShip.BusinessLogic;
using BattleShip.DataLogic;
using BattleShip.UserLogic;
using Newtonsoft.Json;

namespace BattleShip
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var window = new CreatingWindow();
            window.Show();
            //try
            //{
                new ConnectionEstablisher().CreateLobby();
                MessageBox.Show("");
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.ToString());
            //}
            window.Close();
            //new AppLifeCircle().Start();
        }
    }
}
