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
        public byte[] Data { get; } // переделать в массив байтов - пусть форма сама решает, как обработать

        public DataEventArgs(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            Data = data;
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


    public class StartGameEventArgs : EventArgs
    {
        public bool VsHuman { get; }
        public ClearField ClearField { get; }

        public StartGameEventArgs(bool vsHuman, ClearField clearField)
        {
            if (clearField == null)
                throw new ArgumentNullException(nameof(clearField));
            VsHuman = vsHuman;
            ClearField = clearField;
        }
    }
}
