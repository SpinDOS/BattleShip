using System;

namespace BattleShipRendezvousServer.Dependency_Injection
{
    /// <summary>
    /// Interface for entry of ICacheWithPublicPrivateKeys
    /// </summary>
    /// <typeparam name="TPrivateKey">private key of entry</typeparam>
    /// <typeparam name="TPublicKey">public key of entry</typeparam>
    /// <typeparam name="TPassword">password for public key</typeparam>
    /// <typeparam name="TValue">value of entry</typeparam>
    public interface ICacheWithPublicPrivateKeysEntry<TPrivateKey, TPublicKey, TPassword, TValue>
    {
        TPrivateKey PrivateKey { get; }
        TPublicKey PublicKey { get; }
        TPassword Password { get; }
        TValue Value { get; }
        /// <summary>
        /// The entry is removed if time spent after last check by private id 
        /// is greater than SlidingExpirationTime
        /// </summary>
        TimeSpan? SlidingExpirationDelay { get; set; }
        /// <summary>
        /// Trigger when entry is removed
        /// </summary>
        event CacheItemExpirationDelegate<TPrivateKey, TPublicKey, TPassword, TValue> EntryRemoved;
    }

    /// <summary>
    /// Delegate for event of entry removal
    /// </summary>
    /// <param name="privateKey">private key of the entry</param>
    /// <param name="publicKey">public key of the entry</param>
    /// <param name="password">password of the entry</param>
    /// <param name="value">value of the entry</param>
    /// <param name="reason">reason of the removal</param>
    public delegate void CacheItemExpirationDelegate<in TPrivateKey, in TPublicKey, in TPassword, in TValue>
    (TPrivateKey privateKey, TPublicKey publicKey,
        TPassword password, TValue value, CacheEntryRemoveReason reason);

    /// <summary>
    /// Reasons of entry removal
    /// </summary>
    public enum CacheEntryRemoveReason
    {
        /// <summary>
        /// Unknown (not used)
        /// </summary>
        None,
        /// <summary>
        /// Removed by request
        /// </summary>
        Removed,
        /// <summary>
        /// Expired by SlidingExpirationDelay
        /// </summary>
        Expired,
        /// <summary>
        /// Cancelled by token (not used)
        /// </summary>
        TokenExpired,
        /// <summary>
        /// Capacity of cache is overflown (not used)
        /// </summary>
        Capacity,
    }
}
