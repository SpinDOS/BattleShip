using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using NAudio.Wave;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Class for connection to real player on the internet
    /// </summary>
    public class RealConnection : IEnemyConnection, ICommunicationConnection
    {
        #region Fields and private enum

        // type of data in send and receive packet
        protected enum PacketType : byte
        {
            // data is int for determining who shot first
            DecisionWhoShotFirst,
            // data is square of enemy's shot
            ShotSquare,
            // data is result of my shot
            ResultOfShot,
            // data is a collection of enemy's full squares
            SharingFullSquares,
            // no data, packet is a notification that enemy gave up
            GiveUp,
            // data is a some message
            Message,
        }

        // taskcompletionsource to wait for opponent send int to decide who shot first
        protected volatile TaskCompletionSource<int> TcsShotFirst = new TaskCompletionSource<int>();
        // taskcompletionsource to wait for opponent send its shot
        protected volatile TaskCompletionSource<Square> TcsShotFromEnemy = new TaskCompletionSource<Square>();
        // taskcompletionsource to wait for opponent send result of my shot
        protected volatile TaskCompletionSource<ShotEventArgs> TcsResultOfMyShot = new TaskCompletionSource<ShotEventArgs>();
        // cancellationTokenSource to cancel listenting to events of Client
        protected readonly CancellationTokenSource CancelationListning = new CancellationTokenSource();

        // single exception for any operation after disconnect
        private readonly ObjectDisposedException _disposedException = new ObjectDisposedException("Connection is closed");

        #endregion

        #region Constructor

        /// <summary>
        /// Create instance of RealConnection that controls connection to real opponent on the internet
        /// </summary>
        /// <param name="netClientAndListener">Netclient connected to opponent and listener of the netclient</param>
        public RealConnection(NetClientAndListener netClientAndListener)
        {
            // check argument
            if (netClientAndListener == null)
                throw new ArgumentNullException(nameof(netClientAndListener));
            if (netClientAndListener.Client == null)
                throw new ArgumentNullException("netClientAndListener.Client");
            if (netClientAndListener.Listener == null)
                throw new ArgumentNullException("netClientAndListener.Listener");
            if (!netClientAndListener.Client.IsConnected)
                throw new ArgumentException("netClientAndListener.Client is not connected");

            // save to readonly properties
            Client = netClientAndListener.Client;
            Listener = netClientAndListener.Listener;
            // handle listener events
            Listener.NetworkReceiveEvent += NetworkReceiveHandler;
            Listener.PeerDisconnectedEvent += (peer, reason, code) => EnemyDisconnected?.Invoke(this, reason);

            // start collecting events until cancelled
            ThreadPool.QueueUserWorkItem(o =>
            {
                while (!CancelationListning.IsCancellationRequested)
                {
                    Client.PollEvents();
                }
            });

        }

        #endregion

        #region Properties

        /// <summary>
        /// netclient to send messages and poll events
        /// </summary>
        public NetClient Client { get; }

        /// <summary>
        /// netlistener to subscribe event handlers
        /// </summary>
        public EventBasedNetListener Listener { get; }

        /// <summary>
        /// Opponent shared its full squares
        /// </summary>
        public event EventHandler<IEnumerable<Square>> EnemySharedFullSquares;

        /// <summary>
        /// Opponent sent custom message
        /// </summary>
        public event EventHandler<DataEventArgs> MessageReceived;

        /// <summary>
        /// Raised when received packet that can not be succesfully interpreted
        /// </summary>
        public event EventHandler<DataEventArgs> CorruptedPacketReceived;

        /// <summary>
        /// Enemy gave up 
        /// </summary>
        public event EventHandler EnemyGaveUp;

        /// <summary>
        /// Enemy Disconnected
        /// </summary>
        public event EventHandler<DisconnectReason> EnemyDisconnected;

        /// <summary>
        /// Is connection is still established
        /// </summary>
        public bool IsConnected => Client.IsConnected;

        #endregion

        #region IEnemyConnection implementation

        /// <summary>
        /// Communicate with enemy to decide who shoot first
        /// </summary>
        /// <returns>false if enemy shoot first</returns>
        public bool IsMeShootFirst()
        {
            if (!IsConnected)
                throw _disposedException;
            // random number and compare to enemy's number
            // player with greater number shoots first
            int myint, enemyint;
            do
            {
                // random int
                myint = new Random().Next();
                // send my int
                NetDataWriter writer = new NetDataWriter();
                writer.Put((byte) PacketType.DecisionWhoShotFirst);
                writer.Put(myint);
                Client.Peer.Send(writer, SendOptions.ReliableUnordered);
                // wait for event of enemy's int sharing
                enemyint = TcsShotFirst.Task.Result;
                // create new TcsShotFirst for next usages
                TcsShotFirst = new TaskCompletionSource<int>();
            } while (myint == enemyint); // loop if your and enemy's int are equal
            return myint > enemyint;
        }

        /// <summary>
        /// Get shot from enemy
        /// </summary>
        /// <returns>Square of enemy's shot</returns>
        public Square GetShotFromEnemy()
        {
            if (!IsConnected)
                throw _disposedException;
            // wait for event of enemy's shot
            var result = TcsShotFromEnemy.Task.Result;
            // create new TcsShotFromEnemy for next usages
            TcsShotFromEnemy = new TaskCompletionSource<Square>();
            // return result
            return result;
        }

        /// <summary>
        /// Report enemy result of its shot
        /// </summary>
        /// <param name="square">square of enemy's shot</param>
        /// <param name="result">result of enemy's shot</param>
        public void SendStatusOfEnemysShot(Square square, SquareStatus result)
        {
            if (!IsConnected)
                throw _disposedException;
            // encode square and result and send
            Client.Peer.Send(new byte[] {(byte) PacketType.ResultOfShot, square.X, square.Y,
                (byte) result}, SendOptions.ReliableOrdered);
        }

        /// <summary>
        /// Send enemy your shot and wait for result
        /// </summary>
        /// <param name="square">square of your shot</param>
        /// <returns>result of your shot</returns>
        public SquareStatus ShotEnemy(Square square)
        {
            if (!IsConnected)
                throw _disposedException;
            // encode square and send
            Client.Peer.Send(new byte[] { (byte) PacketType.ShotSquare, square.X, square.Y }, SendOptions.ReliableOrdered);
            // wait for event of enemy's answer
            var result = TcsResultOfMyShot.Task.Result;
            // create new TcsResultOfMyShot for next usages
            TcsResultOfMyShot = new TaskCompletionSource<ShotEventArgs>();
            // check if answer square is my shot
            if (result.Square != square)
                throw new AggregateException("Enemy reported result of invalid shot");
            return result.SquareStatus;
        }

        /// <summary>
        /// Send enemy collection of your full squares
        /// </summary>
        /// <param name="fullSquares">collection of your full squares</param>
        public void ShareEnemyMyFullSqures(IEnumerable<Square> fullSquares)
        {
            if (fullSquares == null)
                throw new ArgumentNullException(nameof(fullSquares));
            if (!IsConnected)
                throw _disposedException;

            // get count of squares
            byte count = (byte) fullSquares.Count(); // max number of squares is 100 < byte.MaxValue (255)
            // create array of bytes for squares (2 byte per square) + 
            // packetType flag + squares count
            byte[] arr = new byte[count * 2 + 2];
            // write flag and count of squares
            arr[0] = (byte) PacketType.SharingFullSquares;
            arr[1] =  count;
            // write squares
            int i = 2;
            foreach (var square in fullSquares)
            {
                arr[i++] = square.X;
                arr[i++] = square.Y;
            }
            // send squares
            Client.Peer.Send(arr, SendOptions.ReliableUnordered);
        }

        /// <summary>
        /// Send give up notification
        /// </summary>
        public void GiveUp()
        {
            if (!IsConnected)
                throw _disposedException;
            Client.Peer.Send(new byte[] { (byte) PacketType.GiveUp }, SendOptions.ReliableUnordered);
        }

        /// <summary>
        /// Disconnect enemy and dispose the object
        /// </summary>
        public void Disconnect() => Dispose();

        /// <summary>
        /// Disconnect enemy and dispose the object
        /// </summary>
        public void Dispose()
        {
            if (!IsConnected)
                return;
            // cancel listening for events of netclient
            CancelationListning.Cancel();
            // disconnect
            Client.Disconnect();
        }

        #endregion

        #region ICommunicationConnection implementation

        /// <summary>
        /// Send message to enemy
        /// </summary>
        /// <param name="mesage"></param>
        public void SendMessage(byte[] mesage)
        {
            if (!IsConnected)
                throw _disposedException;
            // add message flag and message data to netdatawriter
            NetDataWriter wr = new NetDataWriter();
            wr.Put((byte) PacketType.Message);
            wr.Put(mesage, 0, mesage.Length);
            // send netdatawriter
            Client.Peer.Send(wr, SendOptions.ReliableUnordered);
        }

        #endregion

        #region NetworkReceiveHandler

        // handle received packet
        private void NetworkReceiveHandler(NetPeer peer, NetDataReader reader)
        {
            // if any error - continue listening but raise notification event
            try
            {
                switch ((PacketType) reader.GetByte())
                {
                    // provide int from packet to task of tcsShotFirst
                    case PacketType.DecisionWhoShotFirst:
                        // wait while main thread create new tcsShotFirst and set result of int from packet
                        while (!TcsShotFirst.TrySetResult(reader.GetInt()))
                            Thread.Sleep(250);
                        break;
                    // if enemy shoot me, provide square from packet to task of tcsShotFromEnemy
                    case PacketType.ShotSquare:
                        // wait while main thread create new tcsShotFromEnemy and set result of square from packet
                        while (!TcsShotFromEnemy.TrySetResult(new Square(reader.GetByte(), reader.GetByte())))
                            Thread.Sleep(250);
                        break;
                    // if enemy reports result of my shot, provide square and status from packet to task of tcsResultOfMyShot
                    case PacketType.ResultOfShot:
                        // wait while main thread create new tcsResultOfMyShot and set result of square from packet
                        while (!TcsResultOfMyShot.TrySetResult(new ShotEventArgs(new Square(reader.GetByte(), reader.GetByte()),
                            (SquareStatus) reader.GetByte())))
                            Thread.Sleep(250);
                        break;
                    // if enemy shares its full squres
                    case PacketType.SharingFullSquares:
                        // read count of squares
                        byte count = reader.GetByte();
                        Square[] arr = new Square[count];
                        // read all squares
                        for (int i = 0; i < count; i++)
                        {
                            arr[i] = new Square(reader.GetByte(), reader.GetByte());
                        }
                        // raise event
                        EnemySharedFullSquares?.Invoke(this, arr);
                        break;
                    // if enemy gave up
                    case PacketType.GiveUp:
                        EnemyGaveUp?.Invoke(this, EventArgs.Empty);
                        break;
                    // if enemy sent custom message
                    case PacketType.Message:
                        MessageReceived?.Invoke(this, new DataEventArgs(reader.Data, 1, reader.Data.Length - 1));
                        break;
                    default:
                        throw new AggregateException("Corrupted message recieved");
                }
            }
            // catch if exception is caused by not enough data to GetXXX or unknown message type
            catch (Exception e) when (e is ArgumentException || "Corrupted message recieved" == (e as AggregateException)?.Message)
            { // raise CorruptedPacketReceived event
                CorruptedPacketReceived?.Invoke(this, new DataEventArgs(reader.Data, 0, reader.Data.Length));
            }
        }

        #endregion
    }
}
