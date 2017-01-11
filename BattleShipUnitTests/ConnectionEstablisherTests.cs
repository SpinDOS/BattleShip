using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using LiteNetLib;
using Xunit;

namespace BattleShipUnitTests
{
    public class ConnectionEstablisherTests
    {
        // check searching random opponent
        [Fact]
        public void RandomSearchTest()
        {
            // get random opponent for client1 in another thread
            NetClient client1;
            Task.Delay(2000).ContinueWith(t => client1 = new ConnectionEstablisher().GetRandomOpponent(CancellationToken.None).Client);
            
            // get opponent for client2 in current thread with cancellation if nobody tries to connect him
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(15000);

            // try get opponent. Ignore exceptions
            NetClient client2 = null;
            try
            {
                client2 = new ConnectionEstablisher().GetRandomOpponent(cts.Token).Client;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            // check if connected
            Assert.True(client2.IsConnected);
        }

        // check if cancellation works
        [Fact]
        public void CheckCancellation()
        {
            // create client1
            NetClient client1;
            var cts1 = new CancellationTokenSource();
            // work of client1 connection will be cancelled after 1 sec after start
            cts1.CancelAfter(2000);
            var task = Task.Run(() => client1 = new ConnectionEstablisher().GetRandomOpponent(cts1.Token).Client);

            Thread.Sleep(750);
            // create client2
            CancellationTokenSource cts2 = new CancellationTokenSource();
            // work of client2 connection will be cancelled after 0.5 sec after start
            cts2.CancelAfter(500);
            NetClient client2 = null;

            try
            {
                // GetRandomOpponent shoult throw operationCancelledException
                client2 = new ConnectionEstablisher().GetRandomOpponent(cts2.Token).Client;
                Assert.Equal(1, 2);
            }
            catch (OperationCanceledException)
            { }

            try
            {
                // task of connecting client1 also should throw aggregateException
                // with innerException of OperationCancelledException
                var r = task.Result;
                Assert.Equal(3, 4);
            }
            catch (AggregateException e) when(e.InnerException is OperationCanceledException)
            { /* ignored */ }

            // check if task has completed due to exception
            Assert.True(task.IsFaulted);
        }

        // create lobby and connect it
        [Fact]
        public void CheckCreateConnectLobby()
        {

            NetClient client1;
            var cts1 = new CancellationTokenSource();
            // reserve ints for info about created lobby
            int publickey = 0, password = 0;
            ConnectionEstablisher establisher1 = new ConnectionEstablisher();
            // save info about lobby to local variables
            establisher1.GotLobbyPublicInfo += (sender, args) =>
            {
                publickey = args.PublicKey;
                password = args.Password;
            };
            // create lobby with cancellation after 15 sec
            var task = Task.Run(() => client1 = establisher1.CreateLobby(cts1.Token).Client);

            CancellationTokenSource cts2 = new CancellationTokenSource();

            // wait a time
            while (password == 0)
            {
                Thread.Sleep(500);
            }

            // try connect lobby with cancellation after 10 sec
            NetClient client2 = new ConnectionEstablisher().ConnectLobby(publickey, password, cts2.Token).Client;
            // check if clients are connected
            Assert.True(client2.IsConnected);
        }
    }
}
