using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{
    public class DataEventArgs : EventArgs
    {
        public byte[] Data { get; }

        public int Offset { get; }

        public int Count { get; }

        /// <summary>
        /// Event args with data byte array
        /// </summary>
        /// <param name="data">Array with data</param>
        public DataEventArgs(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            Data = data;
            Offset = 0;
            Count = data.Length;
        }
        /// <summary>
        /// Event args with data byte array
        /// </summary>
        /// <param name="data">Array with data</param>
        /// <param name="offset">Position where first byte of data is located</param>
        /// <param name="count">Length of the data</param>
        public DataEventArgs(byte[] data, int offset, int count)
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

    /// <summary>
    /// Event args with square
    /// </summary>
    public class SquareEventArgs : RoutedEventArgs
    {
        public Square Square { get; }
        public SquareEventArgs(Square square)
        {
            Square = square;
        }
        public SquareEventArgs(Square square, RoutedEventArgs e) : base(e.RoutedEvent, e.OriginalSource)
        {
            Square = square;
        }
    }

    /// <summary>
    /// Event args with square and status
    /// </summary>
    public class ShotEventArgs : EventArgs
    {
        public Square Square { get; }
        public SquareStatus SquareStatus { get; }

        public ShotEventArgs(Square square, SquareStatus squareStatus)
        {
            Square = square;
            SquareStatus = squareStatus;
        }
    }

    /// <summary>
    /// Event args for starting game
    /// </summary>
    public class StartGameEventArgs : EventArgs
    {
        public bool VsHuman { get; }
        public MyBattleField MyField { get; }

        public StartGameEventArgs(bool vsHuman, MyBattleField myField)
        {
            if (myField == null)
                throw new ArgumentNullException(nameof(myField));
            VsHuman = vsHuman;
            MyField = myField;
        }
    }

    public class AuthentificationEventArgs : EventArgs
    {
        public int PublicKey { get; }
        public int Password { get; }

        public AuthentificationEventArgs(int publicKey, int password)
        {
            PublicKey = publicKey;
            Password = password;
        }
    }
}
