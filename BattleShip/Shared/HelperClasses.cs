using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;

namespace BattleShip.Shared
{
    /// <summary>
    /// Shot container with square and status
    /// </summary>
    public class Shot
    {
        public Square Square { get; }
        public SquareStatus SquareStatus { get; }

        public Shot(Square square, SquareStatus squareStatus)
        {
            Square = square;
            SquareStatus = squareStatus;
        }
    }

    /// <summary>
    /// Incaplulate NetClient and EventBasedNetListener
    /// </summary>
    public class NetClientAndListener
    {
        public NetClient Client { get; }
        public EventBasedNetListener Listener { get; }

        public NetClientAndListener(NetClient client, EventBasedNetListener listener)
        {
            Client = client;
            Listener = listener;
        }
    }

    /// <summary>
    /// Container for data with byte array, starting index of data in this array 
    /// and count of bytes of data
    /// </summary>
    public class DataContainer
    {
        public byte[] Data { get; }

        public int Offset { get; }

        public int Count { get; }

        /// <summary>
        /// Container for data
        /// </summary>
        /// <param name="data">Array with data</param>
        public DataContainer(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            Data = data;
            Offset = 0;
            Count = data.Length;
        }
        /// <summary>
        /// Container for data
        /// </summary>
        /// <param name="data">Array with data</param>
        /// <param name="offset">Position where first byte of data is located</param>
        /// <param name="count">Length of the data</param>
        public DataContainer(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0)
                throw new ArgumentException(nameof(offset));
            if (count < 0)
                throw new ArgumentException(nameof(count));
            if (offset + count > data.Length)
                throw new ArgumentException("Length of data is too small for this offset and count");
            Data = data;
            Offset = offset;
            Count = count;
        }
    }
}
