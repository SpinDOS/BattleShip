using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.BusinessLogic;

namespace BattleShip.Shared
{

    public class MessageEventArgs : RoutedEventArgs
    {
        public string Message { get; }

        public MessageEventArgs(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message is empty");
            Message = message;
        }
        public MessageEventArgs(string message, RoutedEventArgs e) : base(e.RoutedEvent, e.OriginalSource)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message is empty");
            Message = message;
        }

    }

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


    public class StartGameEventArgs : EventArgs
    {
        public bool VsHuman { get; }
        public Field Field { get; }

        public StartGameEventArgs(bool vsHuman, Field field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            VsHuman = vsHuman;
            Field = field;
        }
    }

    public class OnlyBoolEventArgs : EventArgs
    {
        public bool Value { get; }

        public OnlyBoolEventArgs(bool value)
        {
            Value = value;
        }
    }
}
