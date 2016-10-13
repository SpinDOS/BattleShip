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
        private bool EndGame = false;

        public GameWindow()
        {
            InitializeComponent();
        }

        void IPlayerInterface.Start(ClearField clearField)
        {
            if (clearField == null)
                throw new ArgumentNullException(nameof(clearField));
            MyField.IsEnabled = false;
            foreach (var square in clearField.ShipSquares)
                MyField[square].SquareStatus = SquareStatus.Full;
            ((Window)this).ShowDialog();
        }


        public Square GetMyShot()
        {
            Square square = tcs.Task.Result;
            tcs = new TaskCompletionSource<Square>();
            return square;
        }

        public void MarkEnemySquareWithStatus(Square square, SquareStatus status)
        {
            EnemyField.Dispatcher.Invoke(() => EnemyField[square].SquareStatus = status);
        }

        public void MarkMySquareWithStatus(Square square, SquareStatus status)
        {
            MyField.Dispatcher.Invoke(() => MyField[square].SquareStatus = status);
        }

        public void ShowInfo(string info, bool blockInterface)
        {
            if (string.IsNullOrWhiteSpace(info))
                throw new ArgumentNullException(nameof(info));
            EnemyField.Dispatcher.Invoke(() => EnemyField.IsEnabled = !blockInterface);
            Infomation.Dispatcher.Invoke(() => Infomation.Content = info);
            if (!EndGame)
                ProgressBar.Dispatcher.Invoke(() => ProgressBar.Visibility =
                    blockInterface ? Visibility.Visible : Visibility.Collapsed);
        }

        public void ShowGameEnd(bool win)
        {
            EndGame = true;
            string message = "You " + (win ? "win!)" : "lost!(");
            ShowInfo(message, true);
            ProgressBar.Dispatcher.Invoke(() =>ProgressBar.Visibility = Visibility.Collapsed);
            MessageBox.Show(message, "End game", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!EndGame && MessageBox.Show("Are you sure?", "Are your sure?",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                e.Cancel = true;
            else
                InterfaceClose?.Invoke(this, EventArgs.Empty);
        }

        private void EnemyField_Square_Clicked(object sender, SquareEventArgs e)
        { tcs.SetResult(e.Square); }

    }
}
