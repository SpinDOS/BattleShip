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
using NAudio.Wave;

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
            TextMessage,
            AudioMessage,
            EndCallRequest,
            Quit
        }
        
        // enum with state of call
        private enum CallState : byte
        {
            Ready,
            Calling,
            InProgress
        }

        // for click handling to get my shot
        protected volatile TaskCompletionSource<Square> MyShotSource = new TaskCompletionSource<Square>();
        
        // prevent actions after endgame
        protected volatile bool GameEnded = false;

        // prevent actions after enemy disconnected
        protected volatile bool Disconnected = false;

        // encoding for text messages of chat
        protected readonly Encoding TextMessagesEncoding = Encoding.Unicode;

        // CALL SECTION
        //************************************************************************************************************************************

        // backing field for CallNotificationWindow
        private CallNotificationWindow _callNotificationWindow;

        // get or create new CallNotificationWindow
        protected CallNotificationWindow CallNotificationWindow => 
            _callNotificationWindow ?? (_callNotificationWindow = this.Dispatcher.Invoke(() => new CallNotificationWindow() {Owner = this}));

        // bool to detect if the form of asking user to accept or decline call was closed by endCallRequest
        private volatile bool _callCancelled = false;

        // bool to indicate current call state
        private volatile CallState _currentCallState = CallState.Ready;

        // timer to increase call duration every second
        private Timer _callDuraionTimer;
        
        // object that controlls all sounds
        private readonly SoundController _soundController = new SoundController();

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

            // default volume is 0.5
            _soundController.Volume = 0.5f;
            // send recorded packets
            _soundController.SoundRecorded += (sender, args) =>
            {
                // create formatted message
                byte[] arr = new byte[args.BytesRecorded + 1];
                arr[0] = (byte) MessageType.AudioMessage;
                Array.Copy(args.Buffer, 0, arr, 1, args.BytesRecorded);
                // send the message
                this.UserSentMessage?.Invoke(this, new DataEventArgs(arr));
            };
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
            var message = "Opponent gave up";
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
                ? "Opponent disconnected"
                : "Connection problems. Opponent Disconnected";
            // block form and show messageBox
            this.Dispatcher.Invoke(() =>
            {
                BlockFormConnectionProblems(message);
                MessageBox.Show(this, message, "Lost connection to the opponent", MessageBoxButton.OK,
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
            var result = this.Dispatcher.Invoke(() => MessageBox.Show(this, "Keep connection to this opponent for next game?", 
                "Keep connection?", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes);

            // if opponent has disconnected while user was answering the previous question
            if (Disconnected)
            {
                this.Dispatcher.Invoke(() => MessageBox.Show(this, "Anyway opponent has disconnected",
                    "Opponent has disconnected", MessageBoxButton.OK, MessageBoxImage.Exclamation));
                return false;
            }
            return result;
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
        public async void ShowMessage(DataEventArgs data)
        {
            // get message type
            var messageType = (MessageType) data.Data[data.Offset];
            switch (messageType)
            {
                // if starting call or got accept for my call
                case MessageType.CallInitialization:
                    // decide what to do based on current call state
                    switch (_currentCallState)
                    {
                        // if user is waiting for call
                        case CallState.Ready:
                            // ask user if he wants to accept call
                            _callCancelled = false;
                            _soundController.PlayRingtone();
                            bool accept = await this.Dispatcher.InvokeAsync(() => CallNotificationWindow.GetAnswer()); // show window
                            // if call was not accepted
                            if (_callCancelled || !accept)
                                if (!_soundController.IsDisposed) // if the cancel was not caused by enemy quit
                                    _soundController.PlayEndCallSound();
                            // if the window was closed by cancel request - do nothing
                            if (_callCancelled)
                                break;
                            // else - send answer and start call if need
                            if (accept)
                            {// try report accept. if success - start call
                                if (SendMessage(new DataEventArgs(new byte[] {(byte) MessageType.CallInitialization})))
                                    StartCall();
                            }
                            else // report decline
                                SendMessage(new DataEventArgs(new byte[] {(byte) MessageType.EndCallRequest}));

                            break;
                        // if user is already calling and opponent accepted the call
                        case CallState.Calling:
                            StartCall();
                            break;
                        // if call is in progress - do nothing
                        case CallState.InProgress:
                            break;
                    }
                    break;

                // got audio message
                case MessageType.AudioMessage:
                    // if call is not in progress - do nothing
                    if (_currentCallState == CallState.InProgress)
                        PlaySound(new DataEventArgs(data.Data, data.Offset + 1, data.Count - 1));
                    break;
                // got text message

                case MessageType.TextMessage: // if text message - show it ChatWindow
                    // decode message without first byte (used to check message type)
                    var text = "Opponent: " + TextMessagesEncoding.GetString(data.Data, data.Offset + 1, data.Count - 1) 
                        + Environment.NewLine;
                    // add to chat window
                    await ChatWindow.Dispatcher.InvokeAsync(() => ChatWindow.Text += text);
                    break;
                // if opponent sent a request to end call or declined my call


                case MessageType.EndCallRequest:
                    switch (_currentCallState)
                    {
                        // if user is waiting for call
                        case CallState.Ready:
                            // if window to ask to accept or decline call is shown
                            if (CallNotificationWindow.IsVisible)
                            {
                                // mark as canceled and close window
                                _callCancelled = true;
                                CallNotificationWindow.Dispatcher.Invoke(CallNotificationWindow.Hide);
                            }
                            // else - do nothing
                            break;
                        // if i am calling and opponent declined my call
                        case CallState.Calling:
                            ShowCallDeclined();
                            break;
                        // if call is in progress - end it
                        case CallState.InProgress:
                            EndCall();
                            break;
                    }
                    break;


                // if opponent quit the game window
                case MessageType.Quit: // if enemy quit - block chat
                    ChatColumn.Dispatcher.Invoke(() => BlockChat(false));
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
        // return if the message was sent
        private bool SendMessage(DataEventArgs data)
        {
            if (Disconnected)
                return false;
            try // try send
            {
                UserSentMessage?.Invoke(this, data);
                return true;
            }
            catch (ObjectDisposedException) // if enemy disconnected
            {
                // if the form is marked as disconnected
                if (Disconnected)
                    return false;
                // if not - wait a bit and check again
                Thread.Sleep(3000);
                // if still not marked - show error
                if (!Disconnected)
                    ShowError("Can not send message. Opponent disconnected");
                return false;
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
            // report the player that you gave up if game is not ended
            if (!GameEnded)
                MyShotSource.TrySetException(new GiveUpException("Form closed so you gave up"));
            // show info
            BlockFormEndGame("You decided to quit");
            BlockChat(true);
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
            var disposedException = new ObjectDisposedException("The game on the form was marked as ended");
            if (!MyShotSource.TrySetException(disposedException))
            {
                MyShotSource = new TaskCompletionSource<Square>();
                MyShotSource.SetException(disposedException);
            }
        }

        // block chat
        // me is true, if blocking case is my decision to quit
        private void BlockChat(bool me)
        {
            // if already blocked
            if (!BtnSendMessage.IsEnabled)
                return;
            // block chat textbox, send button, call button, volume slider
            BtnSendMessage.IsEnabled = TxtBxMessage.IsEnabled = 
                BtnCall.IsEnabled = SliderVolume.IsEnabled = false;

            // if enemy calls me - cancel asking user to accept or decline
            if (CallNotificationWindow.IsVisible)
            {
                _callCancelled = true;
                CallNotificationWindow.Dispatcher.Invoke(CallNotificationWindow.Close);
            }

            // cancel calling or end call on form
            if (_currentCallState == CallState.Calling)
                CancelCalling();
            else if (_currentCallState == CallState.InProgress) // end call
                EndCall();
            
            // disable sounds
            _soundController.Dispose();
            
            // provide info that enemy quit
            LblCall.Content = string.Empty;
            if (!me)
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
            BlockChat(false);
        }

        #endregion

        #region Audio messaging


        // change waveOut volume on slider volume changed
        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => _soundController.Volume = (float) (e.NewValue / SliderVolume.Maximum);

        // handle btnCall click to start call, cancel calling or end call
        private void BtnCall_Click(object sender, RoutedEventArgs e)
        {
            // if call is not started - change state and send call initialization message
            if (_currentCallState == CallState.Ready)
            {
                _currentCallState = CallState.Calling;
                BtnCall.Content = "Cancel";
                LblCall.Content = "Calling...";

                // play sound
                _soundController.PlayBeeps();
                // send message
                SendMessage(new DataEventArgs(new byte[] {(byte) MessageType.CallInitialization}));
            }
            // if call is in calling state or in progress - send cancel or end call request and show on form
            else
            {
                SendMessage(new DataEventArgs(new byte[] {(byte) MessageType.EndCallRequest}));

                // if calling - cancel of form
                if (_currentCallState == CallState.Calling)
                    CancelCalling();
                // if call is in progress - end it on forn
                else if (_currentCallState == CallState.InProgress)
                    EndCall();
            }
        }

        // show on form end call and stop recording and playing
        private void EndCall()
        {
            if (_currentCallState != CallState.InProgress)
                throw new AggregateException("Can not stop call because it is not started");
            // mark call state as ready for call
            _currentCallState = CallState.Ready;

            // end call
            _soundController.EndCall();
            _callDuraionTimer?.Dispose();

            // show call end on form
            BtnCall.Dispatcher.Invoke(() => BtnCall.Content = "Call");
            // show call ended
            LblCall.Dispatcher.Invoke(() => LblCall.Content = "Call ended");

            // wait 5 secs and if the state has not changed, remove the message
            Task.Delay(5000).ContinueWith(t => LblCall.Dispatcher.Invoke(() =>
            {
                if ("Call ended".Equals(LblCall.Content))
                    LblCall.Content = string.Empty;
            }));
        }

        // cancel calling on form
        private void CancelCalling()
        {
            if (_currentCallState != CallState.Calling)
                throw new AggregateException("Call can not be cancelled because user is not calling");
            _currentCallState = CallState.Ready;

            // update form
            BtnCall.Content = "Call";
            LblCall.Content = string.Empty;

            // stop beeping and play end call sound
            _soundController.PlayEndCallSound();
        }

        // show on form that opponent declined call
        private void ShowCallDeclined()
        {
            if (_currentCallState != CallState.Calling)
                throw new AggregateException("Call can not be declined because user is not calling");
            // mark call state as ready for call
            _currentCallState = CallState.Ready;
            BtnCall.Dispatcher.Invoke(() => BtnCall.Content = "Call");

            // show declined
            LblCall.Dispatcher.Invoke(() => LblCall.Content = "Declined");

            // stop beeping and play end call sound
            _soundController.PlayEndCallSound();

            // wait 5 secs and if the state has not changed, remove the message
            Task.Delay(5000).ContinueWith(t => LblCall.Dispatcher.Invoke(() =>
            {
                if ("Declined".Equals(LblCall.Content))
                    LblCall.Content = string.Empty;
            }));
        }
        
        // show call started on form and start recording and playing
        private void StartCall()
        {
            if (_currentCallState == CallState.InProgress)
                throw new AggregateException("Call is already in progress");
            // mark call state as in progress
            _currentCallState = CallState.InProgress;
            BtnCall.Dispatcher.Invoke(() =>BtnCall.Content = "End call");

            // create long-life variable for duration of the call
            var duration = TimeSpan.Zero;
            // start timer that shows on the form duration of the call
            _callDuraionTimer = new Timer(o => 
            {
                // decide if to show hours
                string hour = duration.Hours == 0 ? "" : duration.Hours.ToString("D2") + ":";
                // show on form
                LblCall.Dispatcher.Invoke(() => LblCall.Content = $"{hour}{duration.Minutes:D2}:{duration.Seconds:D2}");
                // increase duration
                duration += TimeSpan.FromSeconds(1);
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // stop beeping or ringtone if played and start call
            _soundController.StartCall();
        }

        // play sound - add to waveBufer
        private void PlaySound(DataEventArgs data) => _soundController.PlaySound(data);

        #endregion
    }
}
