using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using BattleShipRendezvousServer.Model;

namespace BattleShipRendezvousServer.Middleware
{
    /// <summary>
    /// Memory cache with private and public keys for access value
    /// </summary>
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
        /// Default sliding expiration delay for entries
        /// </summary>
        public TimeSpan? DefaultSlidingExpirationDelay { get; set; } = TimeSpan.FromSeconds(15);

        private TimeSpan _timerExpirationCheckDelay;

        public TimeSpan TimerExpirationCheckDelay
        {
            get { return _timerExpirationCheckDelay; }
            set
            {
                _timerExpirationCheckDelay = value;
                _timer.Change(TimeSpan.Zero, value);
            }
        }

        /// <summary>
        /// Create cache with default TimerExpirationCheckDelay = 60
        /// </summary>
        public MemoryCacheWithPublicPrivateKeys() : this(TimeSpan.FromSeconds(60))
        { }

        /// <summary>
        /// Create cache with TimerExpirationCheckDelay
        /// </summary>
        /// <param name="timerExpirationCheckDelay">How often the object will look for unused entries</param>
        public MemoryCacheWithPublicPrivateKeys(TimeSpan timerExpirationCheckDelay)
        {
            _timerExpirationCheckDelay = timerExpirationCheckDelay;
            _timer = new Timer(TimerCallback, null, TimeSpan.Zero, timerExpirationCheckDelay);
        }

        /// <summary>
        /// Get entry by private key
        /// </summary>
        /// <returns>Full informated entry</returns>
        public ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue> 
            GetEntryByPrivateKey(TPrivateKey privateKey)
        {
            var entry = entries[privateKey];
            // sync and check entry
            if (!entry.Check(true))
                throw new AggregateException("The entry with this private key is expired");
            return entry;
        }

        /// <summary>
        /// Get value by public key and password
        /// </summary>
        /// <param name="publicKey">public key to search</param>
        /// <param name="password">password to confirm public key</param>
        /// <returns></returns>
        public TValue GetValueByPublicKey(TPublicKey publicKey, TPassword password)
        {
            // get private key
            TPrivateKey privateKey = PublicKeyToPrivate[publicKey];
            // get entry
            CacheEntry entry = entries[privateKey];
            // sync and check entry
            if (!entry.Check(false))
                throw new AggregateException("The entry with this public key is expired");

            // check password
            if (!entry.Password.Equals(password))
                throw new AuthenticationException("Bad password");

            // return just value of the entry
            return entry.Value;

        }

        public ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue> CreateEntry(TPrivateKey privateKey, TPublicKey publicKey, TPassword password,
            TValue value)
        {
            // create entry with parameters
            var entry = new CacheEntry(privateKey, publicKey, password, value);

            // remove entry info from dictionaries on expired or remove
            entry.EntryRemoved += (key, key1, password1, value1, reason) =>
            {
                entries.Remove(key);
                PublicKeyToPrivate.Remove(publicKey);
            };
            return entry;
        }

        /// <summary>
        /// Remove entry
        /// </summary>
        /// <param name="privateKey">key of entry to remove</param>
        public void Remove(TPrivateKey privateKey)
        {
            // no need to check - sync and remove
            entries[privateKey].Remove();
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
