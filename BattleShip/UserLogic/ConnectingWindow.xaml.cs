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

        // bool to control if textbox.text changed by user or by code
        private bool changeByUser = true;

        // brush objects for different colors of text on labels
        private readonly Brush _blackLabelBrush, _grayLabelBrush;

        // object to establish connection
        protected ConnectionEstablisher _connectionEstablisher;

        // reference to set result of the operation from event handler
        private volatile NetClientAndListener _result;

        // task of establishing connection. Need to wait it on cancellation
        private volatile Task<NetClientAndListener> _task;

        // mode of search. Need it to prevent asking radiobuttons multiple times
        private volatile SearchMode _searchMode = SearchMode.RandomOpponent;

        // source to cancel search
        private CancellationTokenSource _cancellationOfSearch = new CancellationTokenSource();

        // object for syncing while creating _connectionEstablisher
        private readonly object _objToSync = new object();

        // object to imitate some task created while getting info about server to connect
        private readonly Task<NetClientAndListener> emptyTask = Task.FromResult((NetClientAndListener) null);

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

        #region Public entry point - Start()

        /// <summary>
        /// Show UI and return connected NetClientAndListener
        /// </summary>
        /// <returns>Connected NetClientAndListener or null if cancelled</returns>
        public NetClientAndListener Start()
        { // show dialog and handle events of click
            this.ShowDialog();
            // when window closes, return result or null (if result is not set before)
            return _result;
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
            if (!changeByUser)
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
            changeByUser = false;
            foreach (var badChange in badChanges)
            {
                // revert bad changes
                txtbox.Text = txtbox.Text.Remove(badChange.Offset, badChange.AddedLength);
            }
            // allow handling text change
            changeByUser = true;

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

        private async void MainButton_Click(object sender, RoutedEventArgs e)
        {
            // if task is null = if search progress is not started
            if (_task == null)
            {
                // decide what action to do in task depending on chosen SearchMode
                Func<NetClientAndListener> function = null;
                switch (_searchMode)
                {
                    // if search random opponent
                    case SearchMode.RandomOpponent:
                        function = () => _connectionEstablisher.GetRandomOpponent(_cancellationOfSearch.Token);
                        break;
                    // if craate lobby
                    case SearchMode.CreateLobby:
                        function = () => _connectionEstablisher.CreateLobby(_cancellationOfSearch.Token);
                        break;
                    // if connect lobby
                    case SearchMode.ConnectLobby:
                        // check if there is info about lobby
                        if (string.IsNullOrWhiteSpace(TxtLobbyId.Text) || string.IsNullOrWhiteSpace(TxtPassword.Text))
                        {
                            // show error and return
                            MessageBox.Show("Enter lobby id and password",
                                "Invalid lobby info", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        // textboxes can not contain non-int values (look Textboxes control) 
                        // or too large strings (MaxLength = 8)
                        // get int's of publickey and password
                        int publickey = int.Parse(TxtLobbyId.Text), password = int.Parse(TxtPassword.Text);
                        // connect lobby with this info
                        function =
                            () => _connectionEstablisher.ConnectLobby(publickey, password, _cancellationOfSearch.Token);
                        break;
                    default:
                        throw new AggregateException("Unknow mode to find opponent");
                }
                // getting info about server
                // if connectionEstablisher is null, it can take a while to get serverInfo and check it
                LabelInfo.Content = "Getting info about server";
                // block form
                ChangeStateOnForm(false);

                // set task to not-null object to detect some operation is in progress and cancel it
                _task = emptyTask;

                // try get _connectionEstablisher in another thread
                // save current CancellationTokenSource to local variable because
                // if user cancel the operation, _cancellationOfSearch will be set to new CancellationTokenSource
                // but we need to check current _cancellationOfSearch
                var currectCancellationTokerSource = _cancellationOfSearch;
                bool connectionEstablisherReady = await Task.Run(() => TryGetConnectionEstablisher());

                // if operation is cancelled, return witout any error reports or search
                if (currectCancellationTokerSource.IsCancellationRequested)
                    return;

                // if connectionEstablisher is not ready, report error
                if (!connectionEstablisherReady)
                {
                    // unblock form and show error
                    ChangeStateOnForm(true);
                    MessageBox.Show("Could not find info about working server. Check ServerInfo.json",
                        "Error getting server for finding opponent", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // try set result to returned from connectionEstablisher NetClientAndListener
                try
                {
                    // run establishing connection in task and save it to _task for cancellation delay
                    _task = Task.Run(function);
                    // try get result from task
                    _result = await _task;
                    // if no errors - just close window and return result from Start();
                    this.Close();
                }
                // if could not establish connection
                catch (Exception exception)
                {
                    // prepare form for next search
                    _task = null;
                    _cancellationOfSearch = new CancellationTokenSource();
                    ChangeStateOnForm(true);

                    // provide info about error

                    // another search is in progress
                    if (exception is AggregateException)
                    {
                        MessageBox.Show("Another search is in process", "Search state error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    } // server return bad formatted response
                    else if (exception is FormatException)
                    {
                        MessageBox.Show("Server responded with invalid message", "Server error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    } // timeout of request to the server has expired
                    else if (exception is TimeoutException)
                    {
                        MessageBox.Show("Server does not respond in defined timeout", "Server error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    } // server is unavailable
                    else if (exception is ArgumentException)
                    {
                        MessageBox.Show("Server is unavailable. Check ServerInfo.json", "Server is unavailable", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    } // search cancelled
                    else if (exception is OperationCanceledException)
                    {
                        // ignored as the search cancelled by user
                    } // relative url of connectionEstablisher causes 404 Not Found
                    else if (exception is DirectoryNotFoundException)
                    {
                        MessageBox.Show("Pre-defined url is not found on the server. Check version of the BattleShip application", 
                            "Version dismatch", MessageBoxButton.OK, MessageBoxImage.Error);
                    } // invalid privatekey, publickey or password
                    else if (exception is AuthenticationException)
                    {
                        // if try connect to lobby - bad lobbyId or password
                        if (_searchMode == SearchMode.ConnectLobby)
                            MessageBox.Show("Lobby id with password are not found", "Invalid lobby info",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        else if (_searchMode == SearchMode.CreateLobby )// privatekey not found - internal error
                            MessageBox.Show("Your lobby has been removed by the server", "Lobby not found",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        else if (_searchMode == SearchMode.RandomOpponent) // your opponent was found, but left the search
                            MessageBox.Show("Server has found opponent but he has left the search. " + 
                                "Try start new search for random opponent or crate lobby " + 
                                "and tell someone its LobbyId and password", "Opponent left",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    else // unknown exception
                        throw;
                }
            }

            else // if search is in progress and user wants to cancel it
            {
                // block button to prevent multiple cancellations
                await MainButton.Dispatcher.InvokeAsync(() => MainButton.IsEnabled = false);
                // provide info about cancellation progress
                LabelInfo.Content = "Cancelling";
                // cancel task
                _cancellationOfSearch.Cancel();
                // create new cancellationTokenSource for next tasks
                _cancellationOfSearch = new CancellationTokenSource();

                // wait for task to be cancelled
                try
                {
                    await _task;
                }
                catch { /*ignore any exceptions - they are handled in another await _task*/ }
                // set task to null to tell this object that search is not in progress
                _task = null;
                // unblock form
                ChangeStateOnForm(true);
                // enable button for new search
                await MainButton.Dispatcher.InvokeAsync(() => MainButton.IsEnabled = true);
            }
        }


        // cancel current search progress on close
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _cancellationOfSearch.Cancel();
        }

        #endregion

        #region GUI control

        // eventHandler to provide info to gui about changing status of search
        private async void WriteInfoAboutConnectionStatus(object sender, ConnectionState connectionState)
        {
            // text of LabelInfo
            string newtext = null;

            // set new text of LabelInfo depending on new status of search
            switch (connectionState)
            {
                // ready to start new search
                case ConnectionState.Ready:
                    newtext = "Ready to start";
                    break;

                case ConnectionState.GettingMyPublicIp:
                    newtext = "Getting your public IP";
                    break;

                // search started. getting info from server
                case ConnectionState.GettingInfoFromServer:
                    newtext = "Getting info from server";
                    break;

                // waiting opponent - depends of kind of search
                case ConnectionState.WaitingForOpponent:
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
                            throw new AggregateException("Unknow mode to find opponent");
                    }
                    break;
                // shared my ip and waiting for opponent's ip
                case ConnectionState.WaitingForOpponentsIp:
                    newtext = "Waiting for opponent's IP";
                    break;
                // got opponent's ip and he got my ip - try to connect each other
                case ConnectionState.TryingToConnectP2P:
                    newtext = "Connecting to opponent";
                    break;
                // unknown state
                default:
                    throw new AggregateException("Unknow mode to find opponent");
            }
            // save new text to label
            await LabelInfo.Dispatcher.InvokeAsync(() => LabelInfo.Content = newtext);
        }

        // put form to state of getting info for new search or state of search progress
        private void ChangeStateOnForm(bool active)
        { // active is true if all parts of form is active as search not started

            // enable/disable radiobuttons
            RandomOpponent.IsEnabled = CreateLobby.IsEnabled =
                ConnectLobby.IsEnabled = active;

            // if active - activate form to state of checked radiobutton
            if (active)
            {
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
                        throw new AggregateException("Unknow mode to find opponent");
                }
                // hide progressbar and LabelInfo
                IndeterminateProgressBar.Visibility = Visibility.Hidden;
                this.Height = 210;
            }
            else
            {
                // disable textboxes
                TxtLobbyId.IsEnabled = TxtPassword.IsEnabled = false;
                // enable progress bar and show LabelInfo
                IndeterminateProgressBar.Visibility = Visibility.Visible;
                this.Height = 270;
                // change button text to cancel
                MainButton.Content = "Cancel";
            }
        }

        #endregion

        #region ConnectionEstablisher creating

        // try create connectionEstablisher. If any error happens - return false
        // else set up connectionEstablisher events and return it
        private bool TryGetConnectionEstablisher()
        {
            // lock object to prevent multiple creatings of _connectionEstablisher
            lock (_objToSync)
            {
                // if _connectionEstablisher already exists, return true;
                if (_connectionEstablisher != null)
                    return true;

                // else try create new connectionEstablisher
                try
                {
                    _connectionEstablisher = new ConnectionEstablisher()
                        {RequesInterval = TimeSpan.FromSeconds(5)};
                    // set shorter requestInterval for faster cancellation
                }
                // if error occurs - return false
                catch (Exception exc) when (exc is FileLoadException || exc is InvalidOperationException)
                {
                    return false;
                }

                // if everything is ok, set up events

                // provide just created lobby info for search mode CreateLobby
                _connectionEstablisher.GotLobbyPublicInfo += (o, args) =>
                {
                    TxtLobbyId.Dispatcher.InvokeAsync(() => TxtLobbyId.Text = args.PublicKey.ToString());
                    TxtPassword.Dispatcher.InvokeAsync(() => TxtPassword.Text = args.Password.ToString());
                };

                // provide changes of ConnectionState
                _connectionEstablisher.ConnectionStateChanged += WriteInfoAboutConnectionStatus;
                return true;
            }
        }

        #endregion

    }
}
