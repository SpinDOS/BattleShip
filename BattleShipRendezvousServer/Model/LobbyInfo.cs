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
        public int PublicId { get; }
        public Guid PrivateId { get; }
        public int Password { get; }

        public LobbyInfo(Guid privateId, int publicId, int password)
        {
            PrivateId = privateId;
            PublicId = publicId;
            Password = password;
        }
    }
}
