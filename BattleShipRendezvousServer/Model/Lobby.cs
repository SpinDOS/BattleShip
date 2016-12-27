using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Model
{
    /// <summary>
    /// Lobby class for sharing info between two peers
    /// </summary>
    public class Lobby
    {
        /// <summary>
        /// True, if guest is ready for sharing
        /// </summary>
        public bool GuestReady { get; set; } = false;
        /// <summary>
        /// IpEndpoint of owner of lobby
        /// </summary>
        public IPEndPoint OwnerIEP { get; set; }
        /// <summary>
        /// IpEndpoint of guest
        /// </summary>
        public IPEndPoint GuestIEP { get; set; }
    }
}
