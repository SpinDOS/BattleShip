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
            var cl = new ConnectingWindow().Start();
            MessageBox.Show((cl != null && cl.IsConnected).ToString());
            //var window = new CreatingWindow();
            //window.Show();
            //NetClient c;


            ////if (args.Length == 0)
            ////{
            ////    var est = new ConnectionEstablisher();
            ////    est.GotLobbyPublicInfo +=
            ////        (sender, eventArgs) => Task.Run(() => MessageBox.Show(eventArgs.PublicKey + " " + eventArgs.Password));
            ////    c = est.CreateLobby(CancellationToken.None);
            ////}
            ////else
            ////{
            ////    c = new ConnectionEstablisher().ConnectLobby(int.Parse(args[0]), int.Parse(args[1]), CancellationToken.None);
            ////}


            //c = new ConnectionEstablisher().GetRandomOpponent(CancellationToken.None);

            //MessageBox.Show("OK " + c?.IsConnected);
            //window.Close();
            ////new AppLifeCircle().Start();
        }
    }
}
