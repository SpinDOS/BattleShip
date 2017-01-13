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
using BattleShip.DataLogic;
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
        #region Fields

        // for click handling to get my shot
        protected volatile TaskCompletionSource<Square> MyShotSource = new TaskCompletionSource<Square>();
        
        // prevent actions after endgame
        protected volatile bool GameEnded = false;

        // prevent actions after enemy disconnected
        protected volatile bool Disconnected = false;

        #endregion

        #region Events

        /// <summary>
        /// Trigger when user closes window
        /// </summary>
        public event EventHandler InterfaceForceClose;

        /// <summary>
        /// Trigger when player wants to give up
        /// </summary>
        public event EventHandler GiveUp;

        #endregion

        public GameWindow()
        {
            InitializeComponent();
        }

        #region Public entry points

        /// <summary>
        /// Start game
        /// </summary>
        /// <param name="shipSquares">squares of my ships</param>
        public void Start(IEnumerable<Square> shipSquares, bool pvp)
        {
            // check parameter
            if (shipSquares == null)
                throw new ArgumentNullException(nameof(shipSquares));
            // check state of the form
            if (GameEnded)
                throw new AggregateException("The game of this window has ended");

            IsPvp = pvp;
            // show my ships
            foreach (var square in shipSquares)
                MyField[square].SquareStatus = SquareStatus.Full;

            // show form
            this.ShowDialog();
        }

        /// <summary>
        /// Show or hide chat section
        /// </summary>
        /// <param name="showChat">true, if show chat section</param>
        public void ShowChat(bool showChat)
        {
            // resize window and change chat column width
            if (showChat)
            {
                this.Width = this.MinWidth = 808;
                ChatColumn.Width = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                this.Width = this.MinWidth = 588;
                ChatColumn.Width = new GridLength(0);
            }
        }


        /// <summary>
        /// True, if game vs real human
        /// </summary>
        public bool IsPvp { get; private set; }

        #endregion

        #region IMyShotSource

        /// <summary>
        /// Return next player's shot
        /// </summary>
        /// <returns></returns>
        public Square GetMyShot()
        {
            // get square by event of click
            Square square = MyShotSource.Task.Result;
            MyShotSource = new TaskCompletionSource<Square>();
            return square;
        }

        #endregion

        #region IGameUserInterface

        /// <summary>
        /// Mark square in enemy field
        /// </summary>
        public void MarkEnemySquareWithStatus(Square square, SquareStatus status) => EnemyField.Dispatcher.Invoke(() => EnemyField[square].SquareStatus = status);

        /// <summary>
        /// Mark square in my field
        /// </summary>
        public void MarkMySquareWithStatus(Square square, SquareStatus status) => MyField.Dispatcher.Invoke(() => MyField[square].SquareStatus = status);

        /// <summary>
        /// Provide any info to user in label
        /// </summary>
        /// <param name="info">info to show</param>
        /// <param name="blockInterface">if true, block enemy field</param>
        public void ShowInfo(string info, bool blockInterface)
        {
            // check parameter
            if (string.IsNullOrWhiteSpace(info))
                throw new ArgumentNullException(nameof(info));
            // if game ended - do nothing
            if (GameEnded)
                return;
            // block or unblock enemy field
            EnemyField.Dispatcher.Invoke(() => EnemyField.IsEnabled = !blockInterface);
            // show text
            Infomation.Dispatcher.Invoke(() => Infomation.Text = info);
            // show or hide progress bar
            ProgressBar.Dispatcher.Invoke(() => ProgressBar.Visibility =
                    blockInterface ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// Show info about game end and block form
        /// </summary>
        /// <param name="win">true, if i win</param>
        public void ShowGameEnd(bool win)
        {
            if (GameEnded)
                return;
            string message = "You " + (win ? "win!)" : "lost!(");
            BlockFormEndGame(message);
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

        /// <summary>
        /// Show error and block form
        /// </summary>
        /// <param name="message">message of the error</param>
        public void ShowError(string message)
        {
            if (Disconnected)
                return;
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));
            BlockFormConnectionProblems(message);
            MessageBox.Show(message);
        }

        #endregion

        #region IGameUserPvpInterface

        /// <summary>
        /// Show info that enemy gave up and block form
        /// </summary>
        public void ShowEnemyGaveUp()
        {
            if (GameEnded)
                return;
            var message = "Enemy gave up";
            // cancel my shot source due enemy gave up
            var giveUpException = new GiveUpException(message);
            if (!MyShotSource.TrySetException(giveUpException))
            {
                MyShotSource = new TaskCompletionSource<Square>();
                MyShotSource.SetException(giveUpException);
            }
            // block form and show info
            BlockFormEndGame(message);
            MessageBox.Show(message, "End game", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show info that enemy disconnected
        /// </summary>
        /// <param name="reason">reason of the disconnect</param>
        public void ShowEnemyDisconnected(BattleShipConnectionDisconnectReason reason)
        {
            if (Disconnected)
                return;
            // if i disconnected (not enemy) - do nothing
            if (reason == BattleShipConnectionDisconnectReason.MeDisconnected)
                return;
            if (!IsPvp)
                throw new AggregateException("Local enemy can not disconnect");
            // create message based on reason
            var message = reason == BattleShipConnectionDisconnectReason.EnemyDisconnected
                ? "Enemy disconnected"
                : "Connection problems";

            BlockFormConnectionProblems(message);
            MessageBox.Show(message, "Lost connection to the enemy", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Ask user if the program should keep connection to this enemy for next game
        /// </summary>
        /// <returns></returns>
        public bool AskIfKeepConnection()
        {
            // if already disconnected - return false
            if (Disconnected)
                return false;
            return MessageBox.Show("Keep connection to this enemy for next game?", 
                "Keep connection?", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes;
        }

        #endregion

        #region ICommunicationUserInterface

        public void ShowMessage(DataEventArgs data)
        {
            var text = "Opponent: " + Encoding.Unicode.GetString(data.Data, data.Offset, data.Length) + Environment.NewLine;
            ChatWindow.Dispatcher.Invoke(() => ChatWindow.Text += text);
        }

        public event EventHandler<DataEventArgs> UserSentMessage;

        private void ButtonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            var text = TextBoxMessage.Text;
            TextBoxMessage.Text = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return;
            ChatWindow.Text += "You: " + text + Environment.NewLine;
            UserSentMessage?.Invoke(this, new DataEventArgs(Encoding.Unicode.GetBytes(text)));
        }

        #endregion

        #region Private methods

        // ask before closing and trigger interface close event
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ask the confirmation
            if (!GameEnded && MessageBox.Show("Are you sure want to quit?",
                    "Are your sure want to quit?",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
            {
                // cancel closing
                e.Cancel = true;
                return;
            }
            // show info and call event
            BlockFormEndGame("You decided to quit");
            InterfaceForceClose?.Invoke(this, EventArgs.Empty);
        }


        // provide my next shot
        private void EnemyField_Square_Clicked(object sender, SquareEventArgs e) =>  MyShotSource.TrySetResult(e.Square);

        // trigger gave up event
        private void Btn_GiveUp_Click(object sender, RoutedEventArgs e)
        {
            // ask for confirmation
            if (!GameEnded && MessageBox.Show("Are you sure want to give up?",
                    "Are your sure want to give up?",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
            {
                // cancel
                e.Handled = true;
                return;
            }
            // trigger event
            GiveUp?.Invoke(this, EventArgs.Empty);
            var message = "You gave up";
            // cancel my shot source due enemy gave up
            var giveUpException = new GiveUpException(message);
            if (!MyShotSource.TrySetException(giveUpException))
            {
                MyShotSource = new TaskCompletionSource<Square>();
                MyShotSource.SetException(giveUpException);
            }
            // block form and show info
            BlockFormEndGame(message);
        }

        // blocks form due to game end
        private void BlockFormEndGame(string message)
        {
            if (GameEnded)
                return;
            // mark end game
            GameEnded = true;
            // block give up
            Btn_GiveUp.Dispatcher.Invoke(() => Btn_GiveUp.IsEnabled = false);
            // block shot
            EnemyField.Dispatcher.Invoke(() => EnemyField.IsEnabled = false);
            // show message
            Infomation.Dispatcher.Invoke(() => Infomation.Text = message);
            // hide progress bar
            ProgressBar.Dispatcher.Invoke(() => ProgressBar.Visibility = Visibility.Collapsed);
            // cancel my shot source due to end game
            var disposedException = new ObjectDisposedException("The game on the form was marked as ended");
            if (!MyShotSource.TrySetException(disposedException))
            {
                MyShotSource = new TaskCompletionSource<Square>();
                MyShotSource.SetException(disposedException);
            }
        }

        // blocks form due to game end
        private void BlockFormConnectionProblems(string message)
        {
            if (Disconnected)
                return;
            // mark disconnected
            Disconnected = true;
            // end game
            BlockFormEndGame(message);
            // block chat
            ChatColumn.Dispatcher.Invoke(() => ChatColumn.IsEnabled = false);
        }

        #endregion

    }
}
