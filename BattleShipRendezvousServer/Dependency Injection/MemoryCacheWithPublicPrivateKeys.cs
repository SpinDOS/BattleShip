using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;

namespace BattleShipRendezvousServer.Dependency_Injection
{
    /// <summary>
    /// Memory cache with private and public keys for access value
    /// </summary>
    /// <typeparam name="TPrivateKey">Type of private key</typeparam>
    /// <typeparam name="TPublicKey">Type of public key</typeparam>
    /// <typeparam name="TPassword">Type of password</typeparam>
    /// <typeparam name="TValue">Type of value</typeparam>
    public partial class MemoryCacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue> :
        ICacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue>
    {
        // timer for removing unused entries every expirationCheckDelay
        private Timer _timer;

        // Fast search of private key by public key
        Dictionary<TPublicKey, TPrivateKey> PublicKeyToPrivate = new Dictionary<TPublicKey, TPrivateKey>();

        // Fast search of entry by private key
        Dictionary<TPrivateKey, CacheEntry> entries = new Dictionary<TPrivateKey, CacheEntry>();

        /// <summary>
        /// Create cache with default TimerExpirationCheckDelay = 60
        /// </summary>
        public MemoryCacheWithPublicPrivateKeys()
        {
            _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimerExpirationCheckDelay);
        }

        private TimeSpan? _defaultSlidingExpirationDelay = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Default sliding expiration delay for entries
        /// </summary>
        public TimeSpan? DefaultSlidingExpirationDelay
        {
            get
            {
                lock (this)
                {
                    return _defaultSlidingExpirationDelay;
                }
            }
            set
            {
                lock (this)
                {
                    _defaultSlidingExpirationDelay = value;
                }
            }
        }

        // underlying field for TimerExpirationCheckDelay
        private TimeSpan _timerExpirationCheckDelay = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Delay between checks of entry expirations by timer
        /// </summary>
        public TimeSpan TimerExpirationCheckDelay
        {
            get
            {
                lock (this)
                {
                    return _timerExpirationCheckDelay;
                }
            }
            set
            {
                lock (this)
                {
                    _timerExpirationCheckDelay = value;
                    _timer.Change(TimeSpan.Zero, value); 
                }
            }
        }

        /// <summary>
        /// Count of entries in the cache
        /// </summary>
        public int Count => entries.Count;


        /// <summary>
        /// Get entry by private key
        /// </summary>
        /// <param name="privateKey">key for search</param>
        /// <returns>Entry of the key</returns>
        /// <exception cref="KeyNotFoundException">private key not found</exception>
        ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue>
            ICacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue>.this[TPrivateKey privateKey]
        {
            get
            {
                ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue> result;
                // if found - return, else - throw exception
                if (TryGetEntryByPrivateKey(privateKey, out result))
                    return result;
                throw new KeyNotFoundException($"Private key {privateKey} not found");
            }
        }

        /// <summary>
        /// Try get value by private key 
        /// </summary>
        /// <param name="privateKey">private key to search</param>
        /// <param name="entry">if found, entry of the key</param>
        /// <returns>true, if found</returns>
        public bool TryGetEntryByPrivateKey(TPrivateKey privateKey, out ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue> entry)
        {
            // modify out parameter
            entry = null;
            CacheEntry cacheEntry;
            // try find entry
            if (!entries.TryGetValue(privateKey, out cacheEntry))
                return false;
            // check entry
            if (!cacheEntry.Check(true))
                return false;
            // return entry
            entry = cacheEntry;
            return true;
        }

        /// <summary>
        /// Try get value by public key and password
        /// </summary>
        /// <param name="publicKey">public key to search</param>
        /// <param name="password">password to confirm key</param>
        /// <param name="value">if found, value of the key</param>
        /// <returns>true, if found</returns>
        public bool TryGetValueByPublicKey(TPublicKey publicKey, TPassword password, out TValue value)
        {
            // modify out parameter
            value = default(TValue);
            TPrivateKey privateKey;
            // try find private key
            if (!PublicKeyToPrivate.TryGetValue(publicKey, out privateKey))
                return false;

            // try find entry
            CacheEntry cacheEntry;
            if (!entries.TryGetValue(privateKey, out cacheEntry))
                return false;

            // check password
            if (!cacheEntry.Password.Equals(password))
                throw new AuthenticationException("Bad password");
            // check entry
            if (!cacheEntry.Check(false))
                return false;

            // return value
            value = cacheEntry.Value;
            return true;
        }

        /// <summary>
        /// Get value by public key
        /// </summary>
        /// <param name="publicKey">key for search</param>
        /// <param name="password">password to confirm key</param>
        /// <returns>Value of the entry</returns>
        /// <exception cref="KeyNotFoundException">public key not found</exception>
        /// <exception cref="AuthenticationException">password is not correct</exception>
        TValue ICacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue>.this[TPublicKey publicKey, TPassword password]
        {
            get
            {
                TValue value;
                // if found - return, else - throw exception
                if (TryGetValueByPublicKey(publicKey, password, out value))
                    return value;
                throw new KeyNotFoundException($"Public key {publicKey} not found");
            }
        }

        /// <summary>
        /// Create entry with following parameters
        /// </summary>
        /// <returns>Retrun created entry for modifying SlidingExpirationDelay 
        /// or to listen to event</returns>
        public ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue> CreateEntry(TPrivateKey privateKey, TPublicKey publicKey, TPassword password,
            TValue value)
        {
            // create entry with parameters
            var entry = new CacheEntry(privateKey, publicKey, password, value);

            // initialize sliding expiration
            entry.SlidingExpirationDelay = DefaultSlidingExpirationDelay;

            // remove entry info from dictionaries on expired or remove
            entry.EntryRemoved += (key, key1, password1, value1, reason) =>
            {
                entries.Remove(key);
                PublicKeyToPrivate.Remove(publicKey);
            };

            // add entry to dictionaries
            PublicKeyToPrivate.Add(publicKey, privateKey);
            entries.Add(privateKey, entry);

            return entry;
        }


        /// <summary>
        /// Try remove entry by private key
        /// </summary>
        /// <param name="privateKey">key of entry to remove</param>
        /// <returns>true, if key was found and entry was removed</returns>
        public bool TryRemove(TPrivateKey privateKey)
        {
            // try get entry
            CacheEntry entry;
            bool result = entries.TryGetValue(privateKey, out entry);

            // if found - remove
            if (result)
                entry.Remove();

            return result;
        }


        // method for times to check all entries every TimerExpirationCheckDelay 
        private void TimerCallback(object state)
        {
            // copy keys collection
            var keys = entries.Keys.ToList();

            // handle every key
            foreach (var privateKey in keys)
            {
                CacheEntry entry;
                // if entry with this key is not removed yet
                if (!entries.TryGetValue(privateKey, out entry))
                    continue;
                // check this entry
                entry.Check(false);
            }
        }

        
    }

    
}
