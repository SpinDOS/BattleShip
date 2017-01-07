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
    public class RealConnection : IEnemyConnection, ICommunicationConnection
    {
        protected readonly NetClient client;
        protected readonly EventBasedNetListener listener;

        WaveIn wavein = new WaveIn();
        WaveOut wo = new WaveOut();

        BufferedWaveProvider bf = new BufferedWaveProvider(new WaveFormat(44100, 1));
        public RealConnection(NetClientAndListener netClientAndListener)
        {
            if (netClientAndListener == null)
                throw new ArgumentNullException(nameof(netClientAndListener));
            if (netClientAndListener.Client == null)
                throw new ArgumentNullException("netClientAndListener.Client");
            if (netClientAndListener.Listener == null)
                throw new ArgumentNullException("netClientAndListener.Listener");
            if (!netClientAndListener.Client.IsConnected)
                throw new ArgumentException("netClientAndListener.Client is not connected");
            client = netClientAndListener.Client;
            listener = netClientAndListener.Listener;
            listener.NetworkReceiveEvent += NetworkReceiveHandler;
            listener.PeerDisconnectedEvent += (peer, reason, code) =>
            {
                switch (reason)
                {
                    case DisconnectReason.ConnectionFailed:
                    case DisconnectReason.SocketReceiveError:
                    case DisconnectReason.SocketSendError:
                        EnemyDisconnected?.Invoke(this, BattleShipDisconnectReason.NetworkError);
                        break;
                    case DisconnectReason.DisconnectCalled:
                        break;
                    case DisconnectReason.DisconnectPeerCalled:
                    case DisconnectReason.RemoteConnectionClose:
                        EnemyDisconnected?.Invoke(this, BattleShipDisconnectReason.OpponentDisconnectCalled);
                        break;
                    case DisconnectReason.Timeout:
                        EnemyDisconnected?.Invoke(this, BattleShipDisconnectReason.Timeout);
                        break;
                    default:
                        throw new AggregateException("unknown disconnect reason");
                }
            };
            Task.Run(() =>
            {
                while (!cancelListning.IsCancellationRequested)
                {
                    client.PollEvents();
                }
            }, cancelListning.Token);

            MessageReceived += (sender, args) =>
            {
                bf.AddSamples(args.Data, args.Offset, args.Length);
            };


            wavein.WaveFormat = new WaveFormat(44100, 1);
            wavein.DataAvailable += (sender, args) => SendMessage(args.Buffer);
            wavein.StartRecording();

            wo.Init(bf);
            wo.Play();
            
        }

        public event EventHandler<IEnumerable<Square>> EnemySharedFullSquares;
        public event EventHandler EnemyGaveUp;
        public event EventHandler<BattleShipDisconnectReason> EnemyDisconnected;
        public event EventHandler<DataEventArgs> MessageReceived;
        public bool IsConnected { get; protected set; }

        private volatile TaskCompletionSource<int> WhoShotFirst = new TaskCompletionSource<int>();
        private volatile TaskCompletionSource<Square> getShotFromEnemy = new TaskCompletionSource<Square>();
        private volatile TaskCompletionSource<ShotEventArgs> resultOfShot = new TaskCompletionSource<ShotEventArgs>();

        private readonly CancellationTokenSource cancelListning = new CancellationTokenSource();

        public bool IsMeShotFirst()
        {
            int myint, enemyint;
            do
            {
                myint = new Random().Next();
                NetDataWriter writer = new NetDataWriter();
                writer.Put((byte) MessageType.DecisionWhoShotFirst);
                writer.Put(myint);
                client.Peer.Send(writer, SendOptions.ReliableOrdered);
                enemyint = WhoShotFirst.Task.Result;
                WhoShotFirst = new TaskCompletionSource<int>();
            } while (myint == enemyint);
            return myint > enemyint;
        }

        public Square GetShotFromEnemy()
        {
            return getShotFromEnemy.Task.Result;
        }

        public void SendStatusOfEnemysShot(Square square, SquareStatus result)
        {
            client.Peer.Send(new byte[] {(byte) MessageType.ResultOfShot, square.X, square.Y,
                (byte) result}, 0, 4, SendOptions.ReliableOrdered );
        }

        public SquareStatus ShotEnemy(Square square)
        {
            client.Peer.Send(new byte[] {(byte) MessageType.ShotSquare, square.X, square.Y}, 0, 3, SendOptions.ReliableOrdered );
            var res = resultOfShot.Task.Result;
            return res.SquareStatus;
        }
        
        public void ShareEnemyMyFullSqures(IEnumerable<Square> fullSquares)
        {
            int count = fullSquares.Count();
            byte[] arr = new byte[count * 2 + 2];
            arr[0] = (byte) MessageType.SharingFullSquares;
            arr[1] = (byte) count;
            int i = 2;
            foreach (var square in fullSquares)
            {
                arr[i++] = square.X;
                arr[i++] = square.Y;
            }
        }

        public void SendMessage(byte[] mesage)
        {
            NetDataWriter wr = new NetDataWriter();
            wr.Put((byte) MessageType.Message);
            wr.Put(mesage, 0, mesage.Length);
            client.Peer.Send(wr, SendOptions.ReliableOrdered);
        }


        public void GiveUp()
        {
            client.Peer.Send(new byte[] {(byte) MessageType.GiveUp}, 0, 1, SendOptions.ReliableOrdered );
        }

        public void Disconnect() => Dispose();

        public void Dispose()
        {
            if (!IsConnected)
                return;
            IsConnected = false;
            cancelListning.Cancel();
            if (client.IsConnected)
                client.Disconnect();
        }

        protected enum MessageType : byte
        {
            DecisionWhoShotFirst,
            ShotSquare, 
            ResultOfShot, 
            SharingFullSquares,
            GiveUp,
            Message,
        }

        private void NetworkReceiveHandler(NetPeer peer, NetDataReader reader)
        {
            switch ((MessageType) reader.GetByte())
            {
                case MessageType.DecisionWhoShotFirst:
                    WhoShotFirst.SetResult(reader.GetInt());
                    break;
                case MessageType.ShotSquare:
                    getShotFromEnemy.SetResult(new Square(reader.GetByte(), reader.GetByte()));
                    getShotFromEnemy = new TaskCompletionSource<Square>();
                    break;
                case MessageType.ResultOfShot:
                    resultOfShot.SetResult(new ShotEventArgs(new Square(reader.GetByte(), reader.GetByte()), 
                        (SquareStatus) reader.GetByte() ));
                    resultOfShot = new TaskCompletionSource<ShotEventArgs>();
                    break;
                case MessageType.SharingFullSquares:
                    byte count = reader.GetByte();
                    Square[] arr = new Square[count];
                    for (int i = 0; i < count; i++)
                    {
                        arr[i++] = new Square(reader.GetByte(), reader.GetByte()); 
                    }
                    EnemySharedFullSquares?.Invoke(this, arr);
                    break;
                case MessageType.GiveUp:
                    EnemyGaveUp?.Invoke(this, EventArgs.Empty);
                    break;
                case MessageType.Message:
                    MessageReceived?.Invoke(this, new DataEventArgs(reader.Data, 1, reader.Data.Length - 1));
                    break;
                default:
                    throw new AggregateException("Unknown received message type");
            }
        }
    }
}
