using System;
using BattleShipRendezvousServer.Model;

namespace BattleShipRendezvousServer.Dependency_Injection
{
    /// <summary>
    /// Middleware for finding random opponent
    /// </summary>
    public class RandomOpponentSearch
    {
        // info about enemy waiting for opponent
        private LobbyInfo waitingEnemy = null;

        // lobby collection
        private ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> _lobbies;

        public RandomOpponentSearch(ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> lobbies)
        {
            _lobbies = lobbies;
        }

        /// <summary>
        /// Try get waiting enemy or register me as waiting
        /// </summary>
        /// <param name="lobbyInfo">Info about lobby of waiting enemy</param>
        /// <returns>True, if found enemy. False, if me is waiting for another player</returns>
        public bool TryGetEnemy(out LobbyInfo lobbyInfo)
        {
            // sync object
            lock (this)
            {
                ICacheWithPublicPrivateKeysEntry<Guid, int, int, Lobby> entry;
                // if no one waiting
                if (waitingEnemy == null || !_lobbies.TryGetEntryByPrivateKey(waitingEnemy.PrivateKey, out entry))
                {
                    // generate data for new entry
                    Random rnd = new Random();
                    Guid guid = Guid.NewGuid();
                    int publickey = rnd.Next(10000000, 100000000);
                    int password = rnd.Next(1000, 10000);
                    Lobby lobby = new Lobby();
                    // add entry to lobby collection
                    entry = _lobbies.CreateEntry(guid, publickey, password, lobby);

                    // on entry remove, set waitingEnemy to null if the enemy hasnot changed
                    entry.EntryRemoved += (key, publicKey, i, value, reason) =>
                    {
                        // sync
                        lock (this)
                        {
                            // if enemy has not changed
                            if (waitingEnemy != null && key.Equals(waitingEnemy.PrivateKey))
                                waitingEnemy = null;
                        }
                        
                    };
                    // return created lobbyinfo and add it to waitingEnemy
                    waitingEnemy = lobbyInfo = new LobbyInfo(guid, publickey, password);
                    return false;
                }
                else // waitingEnemy exists
                {
                    // return waitingEnemy
                    lobbyInfo = waitingEnemy;
                    // let next people search for new enemy
                    waitingEnemy = null;
                    return true;
                }
            }

        }
    }
}
