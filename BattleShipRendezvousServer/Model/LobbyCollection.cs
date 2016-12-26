using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace BattleShipRendezvousServer.Model
{
    public class LobbyCollection
    {
        private static object _objForSync = new object();
        private IMemoryCache _cache;
        private LobbyInfo _randromOpponentLobby = null;
        readonly List<LobbyInfo> _lobbies = new List<LobbyInfo>();

        public LobbyCollection(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryGetRandomOpponent(out LobbyInfo lobbyInfo)
        {
            lock (_objForSync)
            {
                if (_randromOpponentLobby != null)
                {
                    lobbyInfo = _randromOpponentLobby;
                    _randromOpponentLobby = null;
                    return true;
                }
                else
                {
                    lobbyInfo = _randromOpponentLobby = CreateLobby();
                    return false;
                }
            }
        }

        public Lobby GetLobbyByPublicId(int publicId, int password) 
            => _lobbies.First(lobbyinfo => lobbyinfo.PublicId == publicId && lobbyinfo.Password == password).Lobby;


        public Lobby GetLobbyByPrivateId(Guid guid) => _cache.Get<LobbyInfo>(guid).Lobby;

        public void RemoveLobby(Guid guid) => _cache.Remove(guid);


        public LobbyInfo CreateLobby()
        {
            LobbyInfo lobbyInfo = new LobbyInfo();
            while (_lobbies.Any(lobbyinfo => lobbyinfo.PublicId == lobbyInfo.PublicId))
            {
                lobbyInfo = new LobbyInfo();
            }
            var entry = _cache.CreateEntry(lobbyInfo.PrivateId);
            entry.Value = lobbyInfo;
            entry.SlidingExpiration = TimeSpan.FromSeconds(30);
            entry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            { EvictionCallback = 
                (key, value, reason, state) => _lobbies.Remove((LobbyInfo) value)
            });
            return lobbyInfo;
        }
    }
}
