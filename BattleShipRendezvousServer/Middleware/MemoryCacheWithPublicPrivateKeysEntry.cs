using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleShipRendezvousServer.Middleware
{
    public partial class MemoryCacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue> :
        ICacheWithPublicPrivateKeys<TPrivateKey, TPublicKey, TPassword, TValue>
    {
        /// <summary>
        /// Entry of cache
        /// </summary>
        protected class CacheEntry : ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue>
        {
            /// <summary>
            /// Create entry with following information
            /// </summary>
            public CacheEntry(TPrivateKey privateKey, TPublicKey publicKey, TPassword password, TValue value)
            {
                PrivateKey = privateKey;
                PublicKey = publicKey;
                Password = password;
                Value = value;
            }
            public TPrivateKey PrivateKey { get; }
            public TPublicKey PublicKey { get; }
            public TPassword Password { get; }
            public TValue Value { get; }

            /// <summary>
            /// The entry is removed if time spent after last check by private id 
            /// is greater than SlidingExpirationTime
            /// </summary>
            public TimeSpan? SlidingExpirationDelay { get; set; }

            /// <summary>
            /// Trigger when entry is removed
            /// </summary>
            public event CacheItemExpirationDelegate<TPrivateKey, TPublicKey, TPassword, TValue> EntryRemoved;

            // date time of last access of the entry by private id (by owner)
            private DateTime lastModify = DateTime.Now;
            private bool removed = false;

            /// <summary>
            /// Check if the entry is expired and trigger event if so
            /// </summary>
            /// <param name="refresh">If true, sliding expiration will start counting from zero</param>
            /// <returns></returns>
            public bool Check(bool refresh)
            {
                // prevent handling if removed
                if (removed)
                    return false;
                // prevent accessing by 2 users or timer
                lock (this)
                {
                    // if expired
                    if (SlidingExpirationDelay.HasValue &&
                                DateTime.Now > lastModify.Add(SlidingExpirationDelay.Value))
                    {
                        // call event, return false
                        EntryRemoved?.Invoke(PrivateKey, PublicKey, Password, Value, CacheEntryRemoveReason.Expired);
                        removed = true;
                        return false;
                    }

                    // if not expired
                    // if need to refresh
                    if (refresh)
                        lastModify = DateTime.Now;

                    return true; 
                }
            }

            /// <summary>
            /// Call event to remove entry
            /// </summary>
            public void Remove()
            {
                // prevent triggering event twice
                if (removed)
                    return;
                // prevent using this object by another threads
                lock (this)
                {// call event to remove entry
                    EntryRemoved?.Invoke(PrivateKey, PublicKey, Password, Value, CacheEntryRemoveReason.Removed); 
                }
                removed = true;
            } 

        }

    }
}
