using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Middleware
{
    /// <summary>
    /// Interface for cache with access to object of TValue by 
    /// TPrivateKey or TPublicKey + TPassword 
    /// </summary>
    interface ICacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue>
    {
        /// <summary>
        /// Get entry by private key
        /// </summary>
        /// <param name="privateKey">key for search</param>
        /// <returns></returns>
        ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue>
            GetEntryByPrivateKey(TPrivateKey privateKey);

        /// <summary>
        /// Get value by public key
        /// </summary>
        /// <param name="publicKey">key for search</param>
        /// <param name="password">password to confirm</param>
        /// <returns></returns>
        TValue GetValueByPublicKey(TPublicKey publicKey, TPassword password);

        /// <summary>
        /// Create entry with following parameters
        /// </summary>
        /// <returns>Retrun created entry for modifying SlidingExpirationDelay 
        /// or to listen to event</returns>
        ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue>
            CreateEntry(TPrivateKey privateKey, TPublicKey publicKey, 
            TPassword password, TValue value);

        /// <summary>
        /// Remove entry by private key
        /// </summary>
        void Remove(TPrivateKey privateKey);

    }
}
