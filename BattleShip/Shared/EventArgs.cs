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
        /// <summary>
        /// Event args with data byte array
        /// </summary>
        public byte[] Data { get; }

        public DataEventArgs(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            Data = data;
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
