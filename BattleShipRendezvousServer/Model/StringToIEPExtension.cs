using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Model
{
    public static class StringToIEPExtension
    {
        public static IPEndPoint ToIpEndPoint(this string s)
        {
            string[] ipIPort = s.Split(':');
            int port;
            IPAddress ipAddress;
            if (ipIPort.Length != 2 || !IPAddress.TryParse(ipIPort[0], out ipAddress)
                || !int.TryParse(ipIPort[1], out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                return null;
            return new IPEndPoint(ipAddress, port);
        }
    }
}
