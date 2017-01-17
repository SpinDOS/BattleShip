using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
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
using System.Windows.Threading;
using BattleShip.DataLogic;
using BattleShip.Shared;
using LiteNetLib;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for ConnectingWindow.xaml
    /// </summary>
    public partial class ConnectingWindow : Window
    {
        #region Fields and private class
        
        private enum SearchMode
        {
            RandomOpponent,
            CreateLobby,
            ConnectLobby,
        }

        private enum SearchProgress
        {
            ReadyToStart,
            Searching,
            Cancelling,
            Connected
        }

        private volatile SearchProgress _connectingProgress = SearchProgress.ReadyToStart;

        // bool to control if textbox.text changed by user or by code
        private bool _changeByUser = true;

        // brush objects for different colors of text on labels
        private readonly Brush _blackLabelBrush, _grayLabelBrush;

        // object to establish connection
        protected ConnectionEstablisher ConnectionEstablisher;

        // reference to set result of the operation from event handler
        private volatile RealConnection _connection;

        // task of establishing connection. Need to wait it on cancellation
        private volatile Task _task;

        // mode of search. Need it to prevent asking radiobuttons multiple times
        private volatile SearchMode _searchMode = SearchMode.RandomOpponent;

        // source to cancel search
        private CancellationTokenSource _cancellationOfSearch = new CancellationTokenSource();

        // object for syncing while creating _connectionEstablisher
        private readonly object _objToSync = new object();

        // bool to detect is form closing to prevent multiple closing handler processing
        private bool _formClosing = false;

        #endregion

        #region Constructor

        public ConnectingWindow()
        {
            InitializeComponent();
            // initialize brushes for labels
            _grayLabelBrush = new SolidColorBrush(new Color()
            { // gray brush for inactive labels
                A = 0xff,
                R = 0x65,
                G = 0x65,
                B = 0x65,
            });
            _blackLabelBrush = new SolidColorBrush(new Color()
            { // black brush for active labels
                A = 0xff,
                R = 0x25,
                G = 0x25,
                B = 0x25,
            });
            // fix somebug when label text is not visible until change IsChecked
            LabelLobbyId.Foreground = LabelPassword.Foreground = _grayLabelBrush;
        }

        #endregion

        #region Public entry points - Start() and ResetConnection()

        /// <summary>
        /// Show UI and return realConnection to enemy that is ready for game
        /// </summary>
        /// <returns>realConnection to enemy that is ready for game or null if cancelled</returns>
        public RealConnection Start()
        { 
            // if connection has been established
            if (_connection != null)
                // // if enemy disconnected but user was not notified
                if (!_connection.IsConnected)
                {
                    // get ready for new search
                    _connection = null;
                    ChangeFormState(SearchProgress.ReadyToStart);
                    // notify and ask if user want to establish new connection
                    if (MessageBox.Show(this, "Previous opponent has disconnected. Do you want to find new opponent?", 
                        "Do you want to find new opponent?", MessageBoxButton.YesNo, 
                        MessageBoxImage.Question) == MessageBoxResult.No)
                        return null;
                }
                else
                {
                    // get task from connection
                    var task = _connection.StartNewGame();
                    // check if opponent is ready now
                    if (task.IsCompleted)
                        return _connection; // return without form show
                    // if opponent is not ready - show form and start waiting for opponent
                    Task.Delay(500).ContinueWith(t => this.Dispatcher.Invoke(() => WaitForOpponentReady(task)));
                }
            // show dialog and handle events of click
            this.ShowDialog();
            // when window closes, return result or null (if result is not set before)
            return _connection;
        }

        /// <summary>
        /// Reset the connection if it exists
        /// </summary>
        public void ResetConnection()
        {
            // if connection exists and form is not shown
            if (_connection != null && !this.Dispatcher.Invoke(() => this.IsVisible))
            {
                // disconnect and forget
                _connection.Disconnect();
                _connection = null;
                ChangeFormState(SearchProgress.ReadyToStart);
            }
        }

        #endregion

        #region Textboxes control

        // prevent not-digit input
        private void TxtLobbyId_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txtbox = sender as TextBox;
            if (txtbox == null)
                throw new AggregateException("This method must be used only for textbox");

            // prevent handling textchange of this method
            if (!_changeByUser)
                return;


            var badChanges = new List<TextChange>();
            // get bad changes
            foreach (var textChange in e.Changes)
            {
                // handle only adding text
                if (textChange.AddedLength <= 0)
                    continue;
                // part of text that has been changed
                var change = txtbox.Text.Substring(textChange.Offset, textChange.AddedLength);

                // check every char in string
                foreach (var ch in change)
                {
                    if (!char.IsDigit(ch))
                    {
                        badChanges.Add(textChange);
                        break;
                    }
                }
            }

            // if only good changes, return
            if (!badChanges.Any())
                return;

            // to prevent handling change in next strings
            _changeByUser = false;
            foreach (var badChange in badChanges)
            {
                // revert bad changes
                txtbox.Text = txtbox.Text.Remove(badChange.Offset, badChange.AddedLength);
            }
            // allow handling text change
            _changeByUser = true;

            // get last change to set CaretIndex
            var last = e.Changes.Last();
            // if lastChange is good, set caretindex to end of change
            // if not - to start of change
            int maxIndex = badChanges.Contains(last) ? last.Offset : last.Offset + last.AddedLength;

            // if new text is shorter, set caterindex to end of text
            txtbox.CaretIndex = maxIndex > txtbox.Text.Length ? txtbox.Text.Length : maxIndex;
        }

        #endregion

        #region CheckBoxes control

        private void RandomOpponent_Checked(object sender, RoutedEventArgs e)
        {
            // change button text
            MainButton.Content = "Find opponent";

            // deactivate labels and textboxes of lobby info. also clear textbox
            TxtLobbyId.IsEnabled = TxtPassword.IsEnabled = false;
            LabelPassword.Foreground = LabelLobbyId.Foreground = _grayLabelBrush;
            TxtLobbyId.Text = TxtPassword.Text = string.Empty;

            // set _searchMode for further use
            _searchMode = SearchMode.RandomOpponent;
        }

        private void CreateLobby_Checked(object sender, RoutedEventArgs e)
        {
            // change button text
            MainButton.Content = "Create lobby";

            // deactivate labels and textboxes of lobby info. also clear textbox
            TxtLobbyId.IsEnabled = TxtPassword.IsEnabled = false;
            LabelPassword.Foreground = LabelLobbyId.Foreground = _grayLabelBrush;
            TxtLobbyId.Text = TxtPassword.Text = string.Empty;

            // set _searchMode for further use
            _searchMode = SearchMode.CreateLobby;
        }

        private void ConnectLobby_Checked(object sender, RoutedEventArgs e)
        {
            // change button text
            MainButton.Content = "Connect lobby";

            // activate labels and textboxes of lobby info
            TxtLobbyId.IsEnabled = TxtPassword.IsEnabled = true;
            LabelPassword.Foreground = LabelLobbyId.Foreground = _blackLabelBrush;

            // set _searchMode for further use
            _searchMode = SearchMode.ConnectLobby;
        }

        #endregion

        #region Start and cancel search

        // handle button click
        private async void MainButton_Click(object sender, RoutedEventArgs e)
        {
            // meaning of button depends on connectionProgress
            switch (_connectingProgress)
            {
                case SearchProgress.ReadyToStart: // if ready to start - start
                    await SearchOpponent();
                    break;
                case SearchProgress.Searching: // if searching - cancel
                    CancelSearch();
                    break;
                case SearchProgress.Connected: // if connected - disconnect
                    Disconnect();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_connectingProgress));
            }
        }

        // search opponent and wait he is ready
        private async Task SearchOpponent()
        {
            if (_connectingProgress != SearchProgress.ReadyToStart)
                throw new AggregateException("Can not start search while not ready for start");

            // decide what action to do in task depending on chosen SearchMode
            Func<NetClientAndListener> function = null;
            switch (_searchMode)
            {
                // if search random opponent
                case SearchMode.RandomOpponent:
                    function = () => ConnectionEstablisher.GetRandomOpponent(_cancellationOfSearch.Token);
                    break;
                // if craate lobby
                case SearchMode.CreateLobby:
                    function = () => ConnectionEstablisher.CreateLobby(_cancellationOfSearch.Token);
                    break;
                // if connect lobby
                case SearchMode.ConnectLobby:
                    // check if there is info about lobby
                    if (string.IsNullOrWhiteSpace(TxtLobbyId.Text) || string.IsNullOrWhiteSpace(TxtPassword.Text))
                    {
                        // show error and return
                        MessageBox.Show(this, "Enter lobby id and password",
                            "Invalid lobby info", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    // textboxes can not contain non-int values (look Textboxes control) 
                    // or too large strings (MaxLength = 8)
                    // get int's of publickey and password
                    int publickey = int.Parse(TxtLobbyId.Text), password = int.Parse(TxtPassword.Text);
                    // connect lobby with this info
                    function =
                        () => ConnectionEstablisher.ConnectLobby(publickey, password, _cancellationOfSearch.Token);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_searchMode));
            }

            // getting info about server
            // if connectionEstablisher is null, it can take a while to get serverInfo and check it
            LabelInfo.Content = "Getting info about server";

            // block form
            ChangeFormState(SearchProgress.Searching);
            

            // try get _connectionEstablisher in another thread
            // if user cancel the operation, the exception will be thrown
            Task<bool> localTaskConnectionEstablisher = null;
            try
            {
                // start task to configure connection establisher and save it to localTask to get result
                // and to _task to await on cancellation
                _task = localTaskConnectionEstablisher = Task.Run(() => TryConfigureConnectionEstablisher())
                    .WithCancellation(_cancellationOfSearch.Token);

                // if can not get connectionEstablisher
                if (!await localTaskConnectionEstablisher)
                {
                    ChangeFormState(SearchProgress.ReadyToStart);
                    MessageBox.Show(this, "Could not find info about working server. Check ServerInfo.json",
                        "Error getting server for finding opponent", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                ChangeFormState(SearchProgress.ReadyToStart);
                return; // if task was cancelled - no notifications
            }
            finally
            { // if _task has not been changed - clear _task
                if (ReferenceEquals(_task, localTaskConnectionEstablisher))
                    _task = null;
            }

            Task<NetClientAndListener> localTaskConnection = null;
            // try find opponent and connect him
            try
            {
                // run establishing connection in task and save it to localTask to get result 
                // and to  _task for cancellation
                _task = localTaskConnection = Task.Run(function);
                // try get connection to opponent
                _connection = new RealConnection(await localTaskConnection);
            }
            // if could not establish connection
            catch (Exception exception)
            {
                // prepare form for next search
                ChangeFormState(SearchProgress.ReadyToStart);

                // provide info about error

                // another search is in progress
                if (exception is AggregateException)
                {
                    MessageBox.Show(this, "Another search is in process", "Search state error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                } // server return bad formatted response
                else if (exception is OperationCanceledException)
                {
                    // ignored as the search cancelled by user
                } // relative url of connectionEstablisher causes 404 Not Found
                else if (exception is FormatException)
                { // server error - forget server
                    ConnectionEstablisher = null;
                    MessageBox.Show(this, "Server responded with invalid message", "Server error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                } // timeout of request to the server has expired
                else if (exception is TimeoutException)
                {// server error - forget server
                    ConnectionEstablisher = null;
                    MessageBox.Show(this,
                        "Server does not respond in defined timeout. You can try restart the application",
                        "Server error", MessageBoxButton.OK, MessageBoxImage.Error);
                } // server is unavailable
                else if (exception is ArgumentException)
                {// server error - forget server
                    ConnectionEstablisher = null;
                    MessageBox.Show(this, "Server is unavailable. Check ServerInfo.json", "Server is unavailable",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                } // search cancelled

                else if (exception is DirectoryNotFoundException)
                {// server error - forget server
                    ConnectionEstablisher = null;
                    MessageBox.Show(this,
                        "Pre-defined url is not found on the server. Check version of the BattleShip application",
                        "Version dismatch", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (exception is AuthenticationException)// invalid privatekey, publickey or password
                {
                    switch (_searchMode)
                    {
                        case SearchMode.ConnectLobby:// if try connect to lobby - bad lobbyId or password
                            MessageBox.Show(this, "Lobby id with password are not found", "Invalid lobby info",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        case SearchMode.CreateLobby:// privatekey not found - internal error
                            MessageBox.Show(this, "Your lobby has been removed by the server", "Lobby not found",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            break;
                        case SearchMode.RandomOpponent:// your opponent was found, but left the search
                            MessageBox.Show(this, "Server has found opponent but he has left the search. " +
                                                  "Try start new search for random opponent or crate lobby " +
                                                  "and tell someone its LobbyId and password", "Opponent left",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(_searchMode));
                    }
                }
                else // unknown exception
                    throw;

                // no further search
                return;
            }
            finally
            { // if _task has not been changed - clear _task
                if (ReferenceEquals(_task, localTaskConnection))
                    _task = null;
            }
            // if no errors - wait for opponent ready
            ChangeFormState(SearchProgress.Connected);
            await WaitForOpponentReady(_connection.StartNewGame());
        }
        
        private async Task WaitForOpponentReady(Task task)
        {
            if (_connectingProgress != SearchProgress.Connected)
                throw new AggregateException("Can not wait for opponent ready while not connected");

            // local copy to keep changes even when original reference has been changed
            var localConnection = _connection;
            // handle opponent disconnect
            bool enemyDisconnected = false;
            // handle enemy disconnect to cancel task and detect who calls disconnect
            EventHandler<BattleShipConnectionDisconnectReason> disconnectHandler =
                (sender, reason) =>
                {
                    enemyDisconnected = reason != BattleShipConnectionDisconnectReason.MeDisconnected;
                    _cancellationOfSearch.Cancel();
                };
            // save for case when _connection is changed while processing
            localConnection.EnemyDisconnected += disconnectHandler;
            // try wait for enemy ready
            try
            {
                await task.WithCancellation(_cancellationOfSearch.Token);
                // if success - hide form and continue
                this.Hide();
            }
            // if disconnect - prepare for next search
            catch (Exception e) when (e is ObjectDisposedException || e is OperationCanceledException)
            {
                // forget connection and prepare form for next search
                _connection = null;
                ChangeFormState(SearchProgress.ReadyToStart);
                _cancellationOfSearch = new CancellationTokenSource();
                // if enemy disconnected - show messagebox
                if (enemyDisconnected)
                    MessageBox.Show(this, "Enemy disconnected", "Connection failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally // forget disconnect handler
            {
                localConnection.EnemyDisconnected -= disconnectHandler;
            }
        }
        
        // put form in cancelling state and cancel search
        private void CancelSearch()
        {
            if (_connectingProgress != SearchProgress.Searching)
                throw new AggregateException("Can not cancel search while search is not started");
            // change connecting progress
            ChangeFormState(SearchProgress.Cancelling);
            // cancel task
            _cancellationOfSearch.Cancel();
            // create new cancellationTokenSource for next tasks
            _cancellationOfSearch = new CancellationTokenSource();
        }

        // disconnect opponent to prevent waiting for his ready
        private void Disconnect()
        {
            if (_connectingProgress != SearchProgress.Connected || _connection == null || !_connection.IsConnected)
                throw new AggregateException("Can not disconnect while not connected to opponent");
            _cancellationOfSearch.Cancel();
            _connection.Disconnect();
            _connection = null;
        }

        // cancel current search progress on close and wait
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            // check if another handler is active
            if (_formClosing)
                return;
            // mark as closing
            _formClosing = true;

            switch (_connectingProgress)
            {
                case SearchProgress.Searching:
                    CancelSearch();
                    break;
                case SearchProgress.Connected:
                    // ask confirmation
                    if (MessageBox.Show(this, "Do you want to disconnect this opponent?", "Disconnect?",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    { // if cancelled
                        _formClosing = false;
                        return;
                    }
                    // else - disconnect
                    Disconnect();
                    break;
            }

            // wait while form will be unblocked 
            if (_task != null)
                try
                {
                    await _task;
                }
                catch { /* ignored*/ }

            // hide form to reuse
            _formClosing = false;
            this.Hide();
        }

        #endregion

        #region GUI control

        // eventHandler to provide info to gui about changing status of search
        private async void WriteInfoAboutConnectionStatus(object sender, ConnectionEstablishingState connectionEstablishingState)
        {
            // dont write text if search is not in progress
            if (_connectingProgress != SearchProgress.Searching)
                return;

            // text of LabelInfo
            string newtext = null;

            // set new text of LabelInfo depending on new status of search
            switch (connectionEstablishingState)
            {
                // ready to start new search
                case ConnectionEstablishingState.Ready:
                    newtext = "Ready to start";
                    break;

                case ConnectionEstablishingState.GettingMyPublicIp:
                    newtext = "Getting your public IP";
                    break;

                // search started. getting info from server
                case ConnectionEstablishingState.GettingInfoFromServer:
                    newtext = "Getting info from server";
                    break;

                // waiting opponent - depends of kind of search
                case ConnectionEstablishingState.WaitingForOpponent:
                    switch (_searchMode)
                    {
                        // if search for random opponent
                        case SearchMode.RandomOpponent:
                            newtext = "Waiting for opponent";
                            break;

                        // if lobby created and waiting for guest
                        case SearchMode.CreateLobby:
                            newtext = "Lobby created";
                            break;

                        // if connected to lobby and waiting for owner to detect me
                        case SearchMode.ConnectLobby:
                            newtext = "Connecting to lobby";
                            break;

                        // unknown mode
                        default:
                            throw new ArgumentOutOfRangeException(nameof(_searchMode));
                    }
                    break;
                // shared my ip and waiting for opponent's ip
                case ConnectionEstablishingState.WaitingForOpponentsIp:
                    newtext = "Waiting for opponent's IP";
                    break;
                // got opponent's ip and he got my ip - try to connect each other
                case ConnectionEstablishingState.TryingToConnectP2P:
                    newtext = "Connecting to opponent";
                    break;
                // unknown state
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionEstablishingState));
            }
            // save new text to label
            await LabelInfo.Dispatcher.InvokeAsync(() =>
            {
                // one more check because while dipatcker invokes, progress can be changed
                if (_connectingProgress == SearchProgress.Searching)
                    LabelInfo.Content = newtext;
            });
        }

        // provide changing connecting progress on form
        private void ChangeFormState(SearchProgress newProgress)
        {
            switch (newProgress)
            {
                case SearchProgress.ReadyToStart: // if ready to start
                    // activate form, hide progress bar and labelInfo
                    if (_connectingProgress == SearchProgress.ReadyToStart)
                        return;
                    MainButton.IsEnabled = true;
                    // change textboxes, textbox labels and button text
                    switch (_searchMode)
                    {
                        case SearchMode.RandomOpponent:
                            RandomOpponent_Checked(null, null);
                            break;
                        case SearchMode.CreateLobby:
                            CreateLobby_Checked(null, null);
                            break;
                        case SearchMode.ConnectLobby:
                            ConnectLobby_Checked(null, null);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(_searchMode));
                    }
                    // hide progressbar and LabelInfo
                    IndeterminateProgressBar.Visibility = Visibility.Hidden;
                    this.Height = 210;
                    // unblock radiobuttons
                    RandomOpponent.IsEnabled = CreateLobby.IsEnabled =
                        ConnectLobby.IsEnabled = true;
                    break;

                case SearchProgress.Searching: // if searching
                    // block form, show progress bar and labelInfo
                    if (_connectingProgress != SearchProgress.ReadyToStart)
                        throw new AggregateException("Can not start search when not ready to start");
                    // change mainbutton text
                    MainButton.Content = "Cancel";
                    // disable textboxes
                    TxtLobbyId.IsEnabled = TxtPassword.IsEnabled = false;
                    // enable progress bar and show LabelInfo
                    IndeterminateProgressBar.Visibility = Visibility.Visible;
                    this.Height = 268;
                    // block radiobuttons
                    RandomOpponent.IsEnabled = CreateLobby.IsEnabled =
                        ConnectLobby.IsEnabled = false;
                    break;

                case SearchProgress.Cancelling: // if cancelling
                    // block main button
                    if (_connectingProgress != SearchProgress.Searching)
                        throw new AggregateException("Can not cancel search that is not started");
                    MainButton.IsEnabled = false;
                    MainButton.Content = LabelInfo.Content = "Cancelling";
                    break;

                case SearchProgress.Connected: // if connected and waiting for opponent's ready
                    // change labelInfo and button text
                    if (_connectingProgress != SearchProgress.Searching)
                        throw new AggregateException("Could not find opponent without searching");
                    // change mainbutton and labelInfo text
                    LabelInfo.Content = "Connected. Waiting opponent";
                    MainButton.Content = "Disconnect";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newProgress));
            }
            // change search progress
            _connectingProgress = newProgress;
        }


        #endregion

        #region ConnectionEstablisher creating

        // try create connectionEstablisher. If any error happens - return false
        // else set up connectionEstablisher events and return it
        private bool TryConfigureConnectionEstablisher()
        {
            // lock object to prevent multiple creatings of _connectionEstablisher
            lock (_objToSync)
            {
                // if _connectionEstablisher already exists, return true;
                if (ConnectionEstablisher != null)
                    return true;

                // else try create new connectionEstablisher
                try
                {
                    ConnectionEstablisher = new ConnectionEstablisher()
                        {RequesInterval = TimeSpan.FromSeconds(5)};
                    // set shorter requestInterval for faster cancellation
                }
                // if error occurs - try default server adress
                catch (Exception exc) when (exc is FileLoadException || exc is InvalidOperationException)
                {
                    try
                    {
                        ConnectionEstablisher = new ConnectionEstablisher("http://battleshiprendezvousserver.apphb.com")
                        { RequesInterval = TimeSpan.FromSeconds(5) };
                    } 
                    // if both adress sources fail, return false
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                }

                // if everything is ok, set up events

                // provide just created lobby info for search mode CreateLobby
                ConnectionEstablisher.GotLobbyPublicInfo += (o, args) =>
                {
                    TxtLobbyId.Dispatcher.InvokeAsync(() => TxtLobbyId.Text = args.PublicKey.ToString());
                    TxtPassword.Dispatcher.InvokeAsync(() => TxtPassword.Text = args.Password.ToString());
                };

                // provide changes of ConnectionEstablishingState
                ConnectionEstablisher.ConnectionStateChanged += WriteInfoAboutConnectionStatus;
                return true;
            }
        }

        #endregion

    }
}
