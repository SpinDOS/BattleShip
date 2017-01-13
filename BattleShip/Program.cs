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
    // bug : if server is placed on the internet, after some request cancellation, server can stop responding. All is OK, if the server is localhost
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // try start game
            try
            {
                new AppLifeCircle().Start();
            }
            // handle any unhandled exceptions
            catch (Exception exception)
            {
                // notify user
                MessageBox.Show(
                    @"Unexpected error occured. The application will be closed. You can try start new instance of the application",
                    @"Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // try write exception info to log file
                try
                {
                    // write date, time, type of exception, inner data and empty new line
                    StringBuilder content = new StringBuilder();
                    content.AppendLine($"Date and time: {DateTime.Now.ToLongDateString()}, {DateTime.Now.ToLongTimeString()}");
                    content.AppendLine($"Exception: {exception.GetType()}");
                    content.AppendLine(JsonConvert.SerializeObject(exception, Formatting.Indented));
                    content.AppendLine();
                    // write to file
                    File.AppendAllText("errors.log", content.ToString());
                }
                catch { /* ignored */ }
            }
        }
    }
}
