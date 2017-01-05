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
            // create cache
            var cache = new BattleShipRendezvousServer.Dependency_Injection.MemoryCacheWithPublicPrivateKeys<Guid, int, int, Lobby>();
            cache.TimerExpirationCheckDelay = TimeSpan.FromSeconds(10);
            cache.DefaultSlidingExpirationDelay = TimeSpan.FromSeconds(5);
            ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> icache = cache;

            // create info for new entry
            Guid guid1 = Guid.NewGuid();
            int publicId1 = 1;
            int password1 = 1;
            Lobby lobby1 = new Lobby();
            var entry1 = icache.CreateEntry(guid1, publicId1, password1, lobby1);

            // check if entry is really added
            Assert.Equal(1, icache.Count);

            // int for monitoring callback call
            int check = 3;
            // change int on entry remove by timer
            entry1.EntryRemoved += (key, publicKey, password, value, reason) => check = 4;
            // wait for timer
            Thread.Sleep(12000);

            // check if int is changed and entry is removed
            Assert.Equal(4, check);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void CheckExistanceOfEntries()
        {
            // create cache
            var cache = new BattleShipRendezvousServer.Dependency_Injection.MemoryCacheWithPublicPrivateKeys<Guid, int, int, Lobby>();
            cache.TimerExpirationCheckDelay = TimeSpan.FromHours(1);
            cache.DefaultSlidingExpirationDelay = TimeSpan.FromHours(1);
            ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> icache = cache;

            // create entry
            Guid guid1 = Guid.NewGuid();
            int publicId1 = 1;
            int password1 = 1;
            Lobby lobby1 = new Lobby();
            // add entry
            icache.CreateEntry(guid1, publicId1, password1, lobby1);

            // check no exception on getting existing entry by private key
            try
            {
                var x = icache[guid1];
            }
            catch (Exception)
            {
                Assert.Equal(1, 2);
            }

            // check no exception on getting existing entry by public key
            try
            {
                var x = icache[publicId1, password1];
            }
            catch (Exception)
            {
                Assert.Equal(3, 4);
            }
            
            // check exception on getting notexisting entry by private key
            try
            {
                var x = icache[Guid.NewGuid()];
                Assert.Equal(5, 6);
            }
            catch (KeyNotFoundException)
            {
            }

            // check exception on getting existing entry by public key
            try
            {
                var x = icache[2, password1];
                Assert.Equal(7, 8);
            }
            catch (KeyNotFoundException)
            {
            }

            // check exception on getting existing entry by bad password
            try
            {
                var x = icache[publicId1, 2];
                Assert.Equal(9, 10);
            }
            catch (KeyNotFoundException)
            {
            }


            ICacheWithPublicPrivateKeysEntry<Guid, int, int, Lobby> entry;
            // check true on getting existing entry by private key
            if (!icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.Equal(11, 12);

            // check false on getting notexisting entry by bad private key
            if (icache.TryGetEntryByPrivateKey(Guid.NewGuid(), out entry))
                Assert.Equal(13, 14);

            Lobby lobby;

            // check true on getting existing entry by public key
            if (!icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(15, 16);

            // check false on getting notexisting entry by private key
            if (icache.TryGetValueByPublicKey(2, password1, out lobby))
                Assert.Equal(17, 18);

            // check exception on getting existing entry by bad password
            bool b = icache.TryGetValueByPublicKey(publicId1, 2, out lobby);
            Assert.False(b);

            // check true on removing existing entry
            if (!icache.TryRemove(guid1))
                Assert.Equal(21, 22);

            // check false on removing notexisting entry
            if (icache.TryRemove(guid1))
                Assert.Equal(21, 22);

            // check false on getting notexisting entry by private key
            if (icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.Equal(23, 24);

            // check false on getting notexisting entry by public key
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(25, 26);
        }

        [Fact]
        public void SlidingExpireationCheck()
        {
            // create cache
            var cache = new BattleShipRendezvousServer.Dependency_Injection.MemoryCacheWithPublicPrivateKeys<Guid, int, int, Lobby>();
            cache.TimerExpirationCheckDelay = TimeSpan.FromHours(1);
            cache.DefaultSlidingExpirationDelay = TimeSpan.FromSeconds(5);
            ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> icache = cache;

            // create entry
            Guid guid1 = Guid.NewGuid();
            int publicId1 = 1;
            int password1 = 1;
            Lobby lobby1 = new Lobby();
            var entry1 = icache.CreateEntry(guid1, publicId1, password1, lobby1);

            Lobby lobby;
            ICacheWithPublicPrivateKeysEntry<Guid, int, int, Lobby> entry;

            // check for existance of entry after 2sec
            Thread.Sleep(2000);
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.True(ReferenceEquals(lobby, lobby1));
            Thread.Sleep(3000);

            // check for notexistance of entry after 5sec by private or public key
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(1, 2);
            if (icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.Equal(3, 4);

            Assert.Equal(0, icache.Count);

            // create new entry
            var entry2 = icache.CreateEntry(guid1, publicId1, password1, lobby1);

            // check for existance of entry after 2sec
            Thread.Sleep(2000);
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.True(ReferenceEquals(lobby, lobby1));
            Thread.Sleep(2000);

            // check for existance of entry after 4sec and check for refreshing
            if (!icache.TryGetEntryByPrivateKey(guid1, out entry))
                Assert.True(ReferenceEquals(entry2, entry));

            // check for existance of entry after 8 sec with refreshing
            Thread.Sleep(4000);
            if (!icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(5, 6);

            // check for notexistance of entry after 10 sec with one refreshing
            Thread.Sleep(2000);
            if (icache.TryGetValueByPublicKey(publicId1, password1, out lobby))
                Assert.Equal(7, 8);

        }
    }
}
