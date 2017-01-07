using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LiteNetLib;
using LiteNetLib.Utils;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window, IGameUserPvpInterface, ICommunicationUserInterface
    {
        // for click handling
        protected volatile TaskCompletionSource<Square> tcs = new TaskCompletionSource<Square>();

        /// <summary>
        /// Trigger when user closes window
        /// </summary>
        public event EventHandler InterfaceForceClose;

        /// <summary>
        /// Trigger when player wants to give up
        /// </summary>
        public event EventHandler GiveUp; 

        // prevent actions after endgame
        private bool EndGame = false;

        public GameWindow()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Return next player's shot
        /// </summary>
        /// <returns></returns>
        public Square GetMyShot()
        {
            // get square by event of click
            Square square = tcs.Task.Result;
            tcs = new TaskCompletionSource<Square>();
            return square;
        }

        /// <summary>
        /// Start game
        /// </summary>
        /// <param name="shipSquares">squares of my ships</param>
        public void Start(IEnumerable<Square> shipSquares)
        {
            // show my ships
            foreach (var square in shipSquares)
                MyField[square].SquareStatus = SquareStatus.Full;

            // show form
            this.ShowDialog();
        }

        /// <summary>
        /// Mark square in enemy field
        /// </summary>
        public void MarkEnemySquareWithStatus(Square square, SquareStatus status)
        {
            EnemyField.Dispatcher.Invoke(() => EnemyField[square].SquareStatus = status);
        }

        /// <summary>
        /// Mark square in my field
        /// </summary>
        public void MarkMySquareWithStatus(Square square, SquareStatus status)
        {
            MyField.Dispatcher.Invoke(() => MyField[square].SquareStatus = status);
        }

        /// <summary>
        /// Provide any info to user in label
        /// </summary>
        /// <param name="blockInterface">if true, block enemy field</param>
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

        /// <summary>
        /// Show info about game end
        /// </summary>
        /// <param name="win">true, if i win</param>
        public void ShowGameEnd(bool win)
        {
            if (EndGame)
                return;
            Btn_GiveUp.Dispatcher.Invoke(() => Btn_GiveUp.IsEnabled = false);
            EndGame = true;
            string message = "You " + (win ? "win!)" : "lost!(");
            ProgressBar.Dispatcher.Invoke(() =>ProgressBar.Visibility = Visibility.Collapsed);
            ShowInfo(message, true);
            MessageBox.Show(message, "End game", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Discovers enemy's ships
        /// </summary>
        /// <param name="fullSquares">enemy's ships squares</param>
        public void ShowEnemyFullSquares(IEnumerable<Square> fullSquares)
        {
            if (fullSquares == null)
                throw new ArgumentNullException(nameof(fullSquares));
            EnemyField.Dispatcher.Invoke(() =>
            {
                foreach (var square in fullSquares)
                    EnemyField[square].SquareStatus = SquareStatus.Full;
            });
        }

        // ask before closing and trigger interface close event
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (EndGame)
                return;
            if (!EndGame && MessageBox.Show("Are you sure want to quit?",
                    "Are your sure want to quit?",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                e.Cancel = true;
            else
            {
                EndGame = true;
                InterfaceForceClose?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ShowChat(bool show)
        {
            if (show)
                ChatColumn.Width = new GridLength(1, GridUnitType.Star);
            else
                ChatColumn.Width = new GridLength(0);
        }

        // provide my next shot
        private void EnemyField_Square_Clicked(object sender, SquareEventArgs e)
        { tcs.SetResult(e.Square); }

        // trigger gave up event
        private void Btn_GiveUp_Click(object sender, RoutedEventArgs e)
        {
            if (!EndGame && MessageBox.Show("Are you sure want to give up?", 
                "Are your sure want to give up?",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                e.Handled = true;
            else
                GiveUp?.Invoke(this, EventArgs.Empty);
        }

        public void ShowEnemyGaveUp()
        {
            throw new NotImplementedException();
        }

        public void ShowError(string message)
        {
            throw new NotImplementedException();
        }

        public void ShowEnemyDisconnected(DisconnectReason reason)
        {
            throw new NotImplementedException();
        }

        public bool AskIfKeepConnection()
        {
            throw new NotImplementedException();
        }

        public void ShowMessage(NetDataReader reader)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<NetDataWriter> UserSentMessage;
    }
}
