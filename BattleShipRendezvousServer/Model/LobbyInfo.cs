using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Model
{
    public class LobbyInfo
    {
        public Lobby Lobby { get; set; }
        public int PublicId { get; }
        public Guid PrivateId { get; }
        public int Password { get; }

        public LobbyInfo()
        {
            Random rnd = new Random();
            PublicId = rnd.Next(100000, 1000000);
            Password = rnd.Next(1000, 10000);
            PrivateId = Guid.NewGuid();
            Lobby = new Lobby();
        }
    }
}
