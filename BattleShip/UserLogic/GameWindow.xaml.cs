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
        #region Fields and private class
        
        // enum with type of message
        protected enum MessageType : byte
        {
            CallInitialization,
            CallInitializationAnswer,
            TextMessage,
            AudioMessage,
            EndCallRequest,
            Quit
        }

        // for click handling to get my shot
        protected volatile TaskCompletionSource<Square> MyShotSource = new TaskCompletionSource<Square>();
        
        // prevent actions after endgame
        protected volatile bool GameEnded = false;

        // prevent actions after enemy disconnected
        protected volatile bool Disconnected = false;

        // encoding for text messages of chat
        protected readonly Encoding TextMessagesEncoding = Encoding.Unicode;

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
        public void Start(IEnumerable<Square> shipSquares)
        {
            // check parameter
            if (shipSquares == null)
                throw new ArgumentNullException(nameof(shipSquares));
            // check state of the form
            if (GameEnded)
                throw new AggregateException("The game of this window has ended");

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
            // block form and show messageBox
            this.Dispatcher.Invoke(() =>
            {
                BlockFormEndGame(message);
                MessageBox.Show(this, message, "End game", MessageBoxButton.OK, MessageBoxImage.Information);
            });
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
            // block form and show messageBox
            this.Dispatcher.Invoke(() =>
            {
                BlockFormConnectionProblems(message);
                MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
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
            // block form and show messageBox
            this.Dispatcher.Invoke(() =>
            {
                BlockFormEndGame(message);
                MessageBox.Show(this, message, "End game", MessageBoxButton.OK, MessageBoxImage.Information);
            });
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
            // create message based on reason
            var message = reason == BattleShipConnectionDisconnectReason.EnemyDisconnected
                ? "Enemy disconnected"
                : "Connection problems. Enemy Disconnected";
            // block form and show messageBox
            this.Dispatcher.Invoke(() =>
            {
                BlockFormConnectionProblems(message);
                MessageBox.Show(this, message, "Lost connection to the enemy", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
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
            return this.Dispatcher.Invoke(() => MessageBox.Show(this, "Keep connection to this enemy for next game?", 
                "Keep connection?", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes);
        }

        #endregion

        #region ICommunicationUserInterface

        /// <summary>
        /// Raised when user sends message
        /// </summary>
        public event EventHandler<DataEventArgs> UserSentMessage;

        /// <summary>
        /// Show message to user
        /// </summary>
        /// <param name="data">array with message</param>
        public void ShowMessage(DataEventArgs data)
        {
            // get message type
            var messageType = (MessageType) data.Data[data.Offset];
            switch (messageType)
            {
                case MessageType.AudioMessage:
                    break;
                case MessageType.TextMessage: // if text message - show it ChatWindow
                    // decode message without first byte (used to check message type)
                    var text = "Opponent: " + TextMessagesEncoding.GetString(data.Data, data.Offset + 1, data.Count - 1) 
                        + Environment.NewLine;
                    // add to chat window
                    ChatWindow.Dispatcher.Invoke(() => ChatWindow.Text += text);
                    break;
                case MessageType.Quit: // if enemy quit - block chat
                    ChatColumn.Dispatcher.Invoke(BlockChat);
                    break;
                default:
                    throw new AggregateException("Unknown message type of GameWindow");
            }
            
        }

        // handle send click
        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            // get text from textbox and clear the textbox
            var text = TxtBxMessage.Text;
            if (string.IsNullOrWhiteSpace(text))
                return;
            TxtBxMessage.Text = string.Empty;
            // copy text to chat window
            ChatWindow.Text += "You: " + text + Environment.NewLine;
            // configure message and send it
            byte[] arr = new byte[TextMessagesEncoding.GetByteCount(text) + 1];
            arr[0] = (byte) MessageType.TextMessage;
            TextMessagesEncoding.GetBytes(text, 0, text.Length, arr, 1);
            SendMessage(new DataEventArgs(arr));
        }

        // send message and handle exception if enemy disconnected
        private void SendMessage(DataEventArgs data)
        {
            if (Disconnected)
                return;
            try // try send
            {
                UserSentMessage?.Invoke(this, data);
            }
            catch (ObjectDisposedException) // if enemy disconnected
            {
                // if the form is marked as disconnected
                if (Disconnected)
                    return;
                // if not - wait a bit and check again
                Thread.Sleep(3000);
                // if still not marked - show error
                if (!Disconnected)
                    ShowError("Can not send message. Enemy disconnected");
            }
        }

        #endregion

        #region Private methods

        // ask before closing and trigger interface close event
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ask the confirmation
            if (!GameEnded && MessageBox.Show(this, "Are you sure want to quit?",
                    "Are your sure want to quit?",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
            {
                // cancel closing
                e.Cancel = true;
                return;
            }
            // show info
            BlockFormEndGame("You decided to quit");
            // notify opponent that you close form
            SendMessage(new DataEventArgs(new byte[] {(byte) MessageType.Quit}));
            // raise event
            InterfaceForceClose?.Invoke(this, EventArgs.Empty);
        }


        // provide my next shot
        private void EnemyField_Square_Clicked(object sender, SquareEventArgs e) =>  MyShotSource.TrySetResult(e.Square);

        // trigger gave up event
        private void BtnGiveUp_Click(object sender, RoutedEventArgs e)
        {
            // ask for confirmation
            if (!GameEnded && MessageBox.Show(this, "Are you sure want to give up?",
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
            BtnGiveUp.IsEnabled = false;
            // block shot
            EnemyField.IsEnabled = false;
            // show message
            Infomation.Text = message;
            // hide progress bar
            ProgressBar.Visibility = Visibility.Collapsed;
            // cancel my shot source due to end game
            var gameStateException = new GameStateException("The game on the form was marked as ended");
            if (!MyShotSource.TrySetException(gameStateException))
            {
                MyShotSource = new TaskCompletionSource<Square>();
                MyShotSource.SetException(gameStateException);
            }
        }

        // block chat
        private void BlockChat()
        {
            // if already blocked
            if (!BtnSendMessage.IsEnabled)
                return;
            // block chat textbox, send button, call button
            BtnSendMessage.IsEnabled = TxtBxMessage.IsEnabled = BtnCall.IsEnabled = false;
            ChatWindow.Text += "Opponent quit";
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
            BlockChat();
        }

        #endregion

        #region Audio messaging

        private void BtnCall_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EndCall()
        { }

        #endregion

    }
}
