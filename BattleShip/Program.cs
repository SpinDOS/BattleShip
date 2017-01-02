using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleShip.BusinessLogic;
using BattleShip.DataLogic;
using BattleShip.UserLogic;
using LiteNetLib;
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
            var fs = new CancellationTokenSource();
            //fs.CancelAfter(150);
                new ConnectionEstablisher().GetRandomOpponent(fs.Token);
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
