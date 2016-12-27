using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using BattleShipRendezvousServer.Dependency_Injection;
using BattleShipRendezvousServer.Model;
using Xunit;

namespace ServerUnitTests
{
    public class CacheTest
    {
        [Fact]
        public void TimerRemoveCheck()
        {
            var cache = new BattleShipRendezvousServer.Dependency_Injection.MemoryCacheWithPublicPrivateKeys<Guid, int, int, Lobby>();
            cache.TimerExpirationCheckDelay = TimeSpan.FromSeconds(10);
            cache.DefaultSlidingExpirationDelay = TimeSpan.FromSeconds(5);
            ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> icache = cache;
            Guid guid1 = Guid.NewGuid();
            int publicId1 = 1;
            int password1 = 1;
            Lobby lobby1 = new Lobby();
            var entry1 = icache.CreateEntry(guid1, publicId1, password1, lobby1);
            Assert.Equal(1, icache.Count);
            int check = 3;
            entry1.EntryRemoved += (key, publicKey, password, value, reason) => check = 4;
            Thread.Sleep(12000);
            Assert.Equal(4, check);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void CheckExistanceOfEntries()
        {
            var cache = new BattleShipRendezvousServer.Dependency_Injection.MemoryCacheWithPublicPrivateKeys<Guid, int, int, Lobby>();
            cache.TimerExpirationCheckDelay = TimeSpan.FromHours(1);
            cache.DefaultSlidingExpirationDelay = TimeSpan.FromHours(1);
            ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> icache = cache;
            Guid guid1 = Guid.NewGuid();
            int publicId1 = 1;
            int password1 = 1;
            Lobby lobby1 = new Lobby();
            icache.CreateEntry(guid1, publicId1, password1, lobby1);
            try
            {
                var x = icache[guid1];
            }
            catch (Exception)
            {
                Assert.Equal(1, 2);
            }
            try
            {
                var x = icache[publicId1, password1];
            }
            catch (Exception)
            {
                Assert.Equal(3, 4);
            }

            try
            {
                var x = icache[Guid.NewGuid()];
                Assert.Equal(5, 6);
            }
            catch (KeyNotFoundException)
            {
            }
            try
            {
                var x = icache[2, password1];
                Assert.Equal(7, 8);
            }
            catch (KeyNotFoundException)
            {
            }

            try
            {
                var x = icache[publicId1, 2];
                Assert.Equal(9, 10);
            }
            catch (AuthenticationException)
            {
            }

            ICacheWithPublicPrivateKeysEntry<Guid, int, int, Lobby> entry;
            if (!icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.Equal(11, 12);
            if (icache.TryGetEntryByPrivateKey(Guid.NewGuid(), out entry))
                Assert.Equal(13, 14);

            Lobby lobby;
            if (!icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(15, 16);
            if (icache.TryGetValueByPublicKey(2, password1, out lobby))
                Assert.Equal(17, 18);
            try
            {
                icache.TryGetValueByPublicKey(publicId1, 2, out lobby);
                    Assert.Equal(19, 20);
            }
            catch (AuthenticationException) { }

            if (!icache.TryRemove(guid1))
                Assert.Equal(21, 22);
            if (icache.TryRemove(guid1))
                Assert.Equal(21, 22);

            if (icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.Equal(23, 24);
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(25, 26);
        }

        [Fact]
        public void SlidingExpireationCheck()
        {
            var cache = new BattleShipRendezvousServer.Dependency_Injection.MemoryCacheWithPublicPrivateKeys<Guid, int, int, Lobby>();
            cache.TimerExpirationCheckDelay = TimeSpan.FromHours(1);
            cache.DefaultSlidingExpirationDelay = TimeSpan.FromSeconds(5);
            ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> icache = cache;
            Guid guid1 = Guid.NewGuid();
            int publicId1 = 1;
            int password1 = 1;
            Lobby lobby1 = new Lobby();
            var entry1 = icache.CreateEntry(guid1, publicId1, password1, lobby1);

            Lobby lobby;
            ICacheWithPublicPrivateKeysEntry<Guid, int, int, Lobby> entry;

            Thread.Sleep(2000);
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.True(ReferenceEquals(lobby, lobby1));
            Thread.Sleep(3000);
            
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(1, 2);
            if (icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.Equal(3, 4);

            Assert.Equal(0, icache.Count);
            var entry2 = icache.CreateEntry(guid1, publicId1, password1, lobby1);
            Thread.Sleep(2000);
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.True(ReferenceEquals(lobby, lobby1));
            Thread.Sleep(2000);

            if (!icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.True(ReferenceEquals(entry2, entry));
            Thread.Sleep(4000);
            if (!icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(5, 6);
            Thread.Sleep(2000);
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(7, 8);

        }
    }
}
