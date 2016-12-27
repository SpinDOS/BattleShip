using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Model
{
    /// <summary>
    /// Lobby class for sharing info between two peers
    /// </summary>
    public class Lobby
    {
        private bool _guestReady = false;

        /// <summary>
        /// True, if guest is ready for sharing
        /// </summary>
        public bool GuestReady
        {
            get { return Volatile.Read(ref _guestReady); }
            set { Volatile.Write(ref _guestReady, value); }
        }

        private IPEndPoint _ownerIEP;

        /// <summary>
        /// IpEndpoint of owner of lobby
        /// </summary>
        public IPEndPoint OwnerIEP
        {
            get { return Volatile.Read(ref _ownerIEP); }
            set { Volatile.Write(ref _ownerIEP, value); }
        }

        private IPEndPoint _guestIEP;

        /// <summary>
        /// IpEndpoint of guest
        /// </summary>
        public IPEndPoint GuestIEP
        {
            get { return Volatile.Read(ref _guestIEP); }
            set { Volatile.Write(ref _guestIEP, value); }
        }
    }
}
