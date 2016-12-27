using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace BattleShipRendezvousServer.Model
{
    public class RandomOpponentSearch
    {
        //private object objToSync = new object();
        //private LobbyCollection _lobbies;
        //private IMemoryCache _cache;
        //private LobbyInfo WaitingPlayer = null;
        //public RandomOpponentSearch(LobbyCollection lobbies, IMemoryCache cache)
        //{
        //    _lobbies = lobbies;
        //    _cache = cache;
        //}

        //public bool TryGetOpponent(out LobbyInfo lobbyInfo)
        //{
        //    bool result = false;
        //    lock (objToSync)
        //    {
        //        if (WaitingPlayer != null)
        //        {
        //            lobbyInfo = WaitingPlayer;
        //            WaitingPlayer = null;
        //            result = true;
        //            _lobbies.GetLobbyByPrivateId();
        //        }
        //        else
        //        {
        //            WaitingPlayer = lobbyInfo = _lobbies.CreateLobby();
        //        }
        //    }
        //    if (result)
        //    {
        //        Lobby lobby = _lobbies.GetLobbyByPublicId
        //            (lobbyInfo.PublicId, lobbyInfo.Password);
        //        lobby.GuestReady = true;
        //    }
        //    else
        //    {
        //        //var entry = _cache.CreateEntry(lobbyInfo.PrivateId);
        //        //entry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
        //        //{
        //        //    EvictionCallback = (key, value, reason, state) =>
        //        //    {
        //        //        lock (objToSync)
        //        //        {
        //        //            if (key.Equals(WaitingPlayer.PrivateId))
        //        //                WaitingPlayer = null; 
        //        //        }
        //        //    }
        //        //});
        //    }
        //    return result;
        //}
    }
}
