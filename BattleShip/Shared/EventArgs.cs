using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{
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
