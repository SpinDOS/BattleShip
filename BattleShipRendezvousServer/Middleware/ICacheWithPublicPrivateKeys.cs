using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Middleware
{
    /// <summary>
    /// Interface for cache with access to object of TValue by 
    /// TPrivateKey or TPublicKey + TPassword 
    /// </summary>
    public interface ICacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue>
    {

        /// <summary>
        /// Count of entries in the cache
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Get entry by private key
        /// </summary>
        /// <param name="privateKey">key for search</param>
        /// <returns>Entry of the key</returns>
        /// <exception cref="KeyNotFoundException">private key not found</exception>
        ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue>
        this[TPrivateKey privateKey] { get; }

        /// <summary>
        /// Try get value by private key 
        /// </summary>
        /// <param name="privateKey">private key to search</param>
        /// <param name="entry">if found, entry of the key</param>
        /// <returns>true, if found</returns>
        bool TryGetEntryByPrivateKey(TPrivateKey privateKey, out ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue> entry);

        /// <summary>
        /// Try get value by public key and password
        /// </summary>
        /// <param name="publicKey">public key to search</param>
        /// <param name="password">password to confirm key</param>
        /// <param name="value">if found, value of the key</param>
        /// <returns>true, if found</returns>
        bool TryGetValueByPublicKey(TPublicKey publicKey, TPassword password, out TValue value);

        /// <summary>
        /// Get value by public key
        /// </summary>
        /// <param name="publicKey">key for search</param>
        /// <param name="password">password to confirm key</param>
        /// <returns>Value of the entry</returns>
        /// <exception cref="KeyNotFoundException">public key not found</exception>
        /// <exception cref="AuthenticationException">password is not correct</exception>
        TValue this[TPublicKey publicKey, TPassword password] { get; }

        /// <summary>
        /// Create entry with following parameters
        /// </summary>
        /// <returns>Retrun created entry for modifying SlidingExpirationDelay 
        /// or to listen to event</returns>
        ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue>
            CreateEntry(TPrivateKey privateKey, TPublicKey publicKey, 
            TPassword password, TValue value);

        /// <summary>
        /// Try remove entry by private key
        /// </summary>
        /// <param name="privateKey">key of entry to remove</param>
        /// <returns>true, if key was found and entry was removed</returns>
        bool TryRemove(TPrivateKey privateKey);

    }
}
