using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Model
{
    /// <summary>
    /// Incapsulate info about lobby
    /// </summary>
    public class LobbyInfo
    {
        public int PublicKey { get; }
        public Guid PrivateKey { get; }
        public int Password { get; }

        public LobbyInfo(Guid privateKey, int publicKey, int password)
        {
            PrivateKey = privateKey;
            PublicKey = publicKey;
            Password = password;
        }
    }
}
