using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window, IPVPInterface
    {
        volatile TaskCompletionSource<Square> tcs = new TaskCompletionSource<Square>(); 
        public event EventHandler InterfaceClose;
        public GameWindow()
        {
            InitializeComponent();
            this.MyField.Buttons.IsEnabled = false;
        }

        void IPlayerInterface.Start(Field field)
        {
            foreach (var square in field.ShipSquares)
                MyField[square].SquareStatus = SquareStatus.Full;
            //((Window)this).Show();
        }

        public void ShowGameEnd(bool win)
        {
            Dispatcher.Invoke(() => EnemyField.Buttons.IsEnabled = true);
            
        }

        public Square GetMyShot()
        {
            Square square = new Square();
            Dispatcher.Invoke(() =>
            {
                EnemyField.Buttons.IsEnabled = true;
            });
                square = tcs.Task.Result;
                tcs = new TaskCompletionSource<Square>();
            return square;
        }

        public void MarkSquareWithStatus(Square square, SquareStatus status, bool myField)
        {
            Dispatcher.Invoke(() =>
            {
                if (myField)
                    MyField[square].SquareStatus = status;
                else
                {
                    EnemyField[square].SquareStatus = status;
                    if (status == SquareStatus.Miss)
                        BlockWithMessage("You missed. Enemy's turn to shoots");
                }
            });
        }

        public void EnemyDisconnected()
        {
            Dispatcher.Invoke(() => this.IsEnabled = false);
            MessageBox.Show("Enemy disconnected", "Enemy disconnected",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Are your sure?",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                e.Cancel = true;
            else
                InterfaceClose?.Invoke(this, EventArgs.Empty);
        }

        private void EnemyField_Square_Clicked(object sender, SquareEventArgs e)
        {
            BlockWithMessage("Waiting for response");
            tcs.SetResult(e.Square);
        }

        private void BlockWithMessage(string message)
        {
            EnemyField.Buttons.IsEnabled = false;
            //
        }
    }
}
