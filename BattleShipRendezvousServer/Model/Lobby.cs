using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Model
{
    public class Lobby
    {
        public bool GuestReady { get; set; } = false;
        public IPEndPoint OwnerIEP { get; set; }
        public IPEndPoint GuestIEP { get; set; }
    }
}
