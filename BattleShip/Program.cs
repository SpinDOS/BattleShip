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
using BattleShip.Shared;
using BattleShip.UserLogic;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;

namespace BattleShip
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new AppLifeCircle().Start();
        }
    }
}
