using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Shared
{
    public static class StringToIEPExtension
    {
        /// <summary>
        /// Decode IPEndPoint from string
        /// </summary>
        /// <returns>IPEndPoint if s contains IPEndPoint, else return null</returns>
        public static IPEndPoint ToIpEndPoint(this string s)
        {
            // get ip and port as strings
            string[] ipIPort = s.Split(':');
            int port;
            IPAddress ipAddress;
            // try parse strings to ipadress and port
            if (ipIPort.Length != 2 || !IPAddress.TryParse(ipIPort[0], out ipAddress)
                || !int.TryParse(ipIPort[1], out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                return null;
            // return IPEndPoint
            return new IPEndPoint(ipAddress, port);
        }
    }
}
