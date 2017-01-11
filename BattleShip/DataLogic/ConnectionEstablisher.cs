using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BattleShip.Properties;
using BattleShip.Shared;
using LiteNetLib;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Establish P2P connection with special server
    /// </summary>
    public class ConnectionEstablisher
    {
        #region Fields

        // readonly exception for any server response format error
        private readonly FormatException _formatException = new FormatException("Bad format of server response");

        // volatile field for multi-thread cancellation of request
        protected volatile HttpWebRequest _request;
        
        // adress and port of working stun server
        private Tuple<string, int> _goodStunServer = null;
        
        // backing field for ConnectionEstablishingState for volatile access
        private volatile ConnectionEstablishingState _connectionEstablishingState = DataLogic.ConnectionEstablishingState.Ready;

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize class with server adress from ServerInfo.json and check connection to the server
        /// </summary>
        /// <exception cref="FileLoadException">Can not read data from ServerInfo.json</exception>
        /// <exception cref="ArgumentException">ServerInfo.json does not contain adress of valid server</exception>
        public ConnectionEstablisher()
        {
            // try read data
            string text;
            try
            {
                text = File.ReadAllText("ServerInfo.json");
            }
            // throw if can not read data
            catch (Exception e)
            {
                if (e is FileNotFoundException || e is IOException ||
                    e is UnauthorizedAccessException || e is SecurityException)
                    throw new FileLoadException("Could not load data from ServerInfo.json");
                throw;
            }

            // get server adress
            dynamic dyn = JsonConvert.DeserializeObject(text);

            // check adress
            CheckAdress((string) dyn.ServerAdress);
        }

        /// <summary>
        /// Initialize class with server adress and check connection to the server
        /// </summary>
        /// <param name="adress">adress of the server</param>
        /// <exception cref="ArgumentException">adress is not an adress of valid server</exception>
        public ConnectionEstablisher(string adress)
        { // check adress
            CheckAdress(adress); 
        }
        
        // check adress and throw exception if server by the adress is not available
        private void CheckAdress(string adress)
        {
            // try connect to server
            try
            {
                var response = WebRequest.Create(adress).GetResponse();
                response.Close();
            }
            // if can not connect to server
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ConnectFailure)
                    throw new InvalidOperationException("Server is unavailable");
                // if any other status, server is ok => ignore exception
            }
            // if bad uri 
            catch (Exception e) when (e is NotSupportedException || e is UriFormatException)
            {
                throw new InvalidOperationException("Invalid server adress");
            }

            ServerAdress = adress;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Adress of server to find opponent
        /// </summary>
        protected string ServerAdress { get; private set; }

        /// <summary>
        /// Return state of finding opponent process
        /// </summary>
        public ConnectionEstablishingState ConnectionEstablishingState
        {
            get { return _connectionEstablishingState; }
            protected set
            {
                _connectionEstablishingState = value;
                ConnectionStateChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Interval between requests to server to check if opponent found
        /// </summary>
        public TimeSpan RequesInterval { get; set; } = TimeSpan.FromSeconds(7.5);

        public event EventHandler<AuthentificationEventArgs> GotLobbyPublicInfo;
        public event EventHandler<ConnectionEstablishingState> ConnectionStateChanged;

        #endregion

        #region Public methods of creating connection

        /// <summary>
        /// Gets random opponent
        /// </summary>
        /// <param name="ct">token to cancel process</param>
        /// <returns>netclient and listener connected to found opponent</returns>
        /// <exception cref="AggregateException">thrown if another opponent search progress is in use</exception>
        /// <exception cref="FormatException">thrown if server returns response in bad format</exception>
        /// <exception cref="TimeoutException">thrown if server has not responded during timeout</exception>
        /// <exception cref="ArgumentException">thrown if server from is unavailable</exception>
        /// <exception cref="OperationCanceledException">thrown if operation cancelled by user</exception>
        /// <exception cref="DirectoryNotFoundException">thrown if pre-defined relative url is unavailable</exception>
        /// <exception cref="AuthenticationException">thrown if RequestInterval is too large 
        /// and server delete created lobby or another server authentification error</exception>
        public NetClientAndListener GetRandomOpponent(CancellationToken ct)
        {
            // Change Connection state
            if (ConnectionEstablishingState != ConnectionEstablishingState.Ready)
                throw new AggregateException("Opponent search is already in progress");
            ConnectionEstablishingState = ConnectionEstablishingState.GettingInfoFromServer;

            // abort request on cancellation
            ct.Register(() => _request?.Abort());

            // reset ConnectionEstablishingState on exception
            try
            {
                // get response
                var response = MakeRequest(@"/api/randomOpponent", "GET", null);

                // check if cancelled
                ct.ThrowIfCancellationRequested();

                if (response == null)
                    throw _formatException;

                // check response format
                bool found;
                try
                {
                    found = response.found;
                }
                catch (RuntimeBinderException)
                {
                    throw _formatException;
                }

                if (found)
                {
                    int publickey;
                    int password;

                    // check format
                    try
                    {
                        publickey = response.publicKey;
                        password = response.password;
                    }
                    catch (RuntimeBinderException)
                    {
                        throw _formatException;
                    }

                    // change connection state to ready 
                    // to pass initial check in ConnectLobby
                    ConnectionEstablishingState = ConnectionEstablishingState.Ready;
                    return ConnectLobby(publickey, password, ct);
                }
                else
                {
                    // check format
                    Guid privatekey;
                    try
                    {
                        privatekey = response.privateKey;
                    }
                    catch (RuntimeBinderException)
                    {
                        throw _formatException;
                    }

                    return ControlMyLobby(privatekey, ct);
                }

            }
            finally { ConnectionEstablishingState = ConnectionEstablishingState.Ready;}
        }

        /// <summary>
        /// Create lobby and wait for opponent
        /// </summary>
        /// <param name="ct">token to cancel process</param>
        /// <returns>Netclient and listener connected to opponent</returns>
        /// <exception cref="AggregateException">thrown if another opponent search progress is in use</exception>
        /// <exception cref="FormatException">thrown if server returns response in bad format</exception>
        /// <exception cref="TimeoutException">thrown if server has not responded during timeout</exception>
        /// <exception cref="ArgumentException">thrown if server from is unavailable</exception>
        /// <exception cref="OperationCanceledException">thrown if operation cancelled by user</exception>
        /// <exception cref="DirectoryNotFoundException">thrown if pre-defined relative url is unavailable</exception>
        /// <exception cref="AuthenticationException">thrown if RequestInterval is too large and server delete created lobby</exception>
        public NetClientAndListener CreateLobby(CancellationToken ct)
        {
            // Change Connection state
            if (ConnectionEstablishingState != ConnectionEstablishingState.Ready)
                throw new AggregateException("Opponent is already found");
            ConnectionEstablishingState = ConnectionEstablishingState.GettingInfoFromServer;

            // abort request on cancellation
            ct.Register(() => _request?.Abort());

            // reset ConnectionEstablishingState on exception
            try
            {
                dynamic response = MakeRequest(@"/api/lobby/create", "GET", null);
                Guid privatekey;
                int publickey, password;
                try
                {
                    privatekey = response.privateKey;
                    publickey = response.publicKey;
                    password = response.password;
                }
                catch (RuntimeBinderException)
                {
                    throw _formatException;
                }

                GotLobbyPublicInfo?.Invoke(this, new AuthentificationEventArgs(publickey, password));

                return ControlMyLobby(privatekey, ct);
            }
            finally { ConnectionEstablishingState = ConnectionEstablishingState.Ready;}

        }

        /// <summary>
        /// Connect to opponent through the existing lobby
        /// </summary>
        /// <param name="publickey">public key of the lobby</param>
        /// <param name="password">password of the lobby</param>
        /// <param name="ct">token to cancel process</param>
        /// <returns>Netclient and listener connected to opponent</returns>
        /// <exception cref="AggregateException">thrown if another opponent search progress is in use</exception>
        /// <exception cref="FormatException">thrown if server returns response in bad format</exception>
        /// <exception cref="TimeoutException">thrown if server has not responded during timeout</exception>
        /// <exception cref="ArgumentException">thrown if server from is unavailable</exception>
        /// <exception cref="OperationCanceledException">thrown if operation cancelled by user</exception>
        /// <exception cref="DirectoryNotFoundException">thrown if pre-defined relative url is unavailable</exception>
        /// <exception cref="AuthenticationException">thrown if publickey or password are not found in the server</exception>
        public NetClientAndListener ConnectLobby(int publickey, int password, CancellationToken ct)
        {
            // Change Connection state
            if (ConnectionEstablishingState != ConnectionEstablishingState.Ready)
                throw new AggregateException("Opponent is already found");
            ConnectionEstablishingState = ConnectionEstablishingState.WaitingForOpponent;

            // abort request on cancellation
            ct.Register(() => _request?.Abort());

            // reset ConnectionEstablishingState on exception
            try
            {
                ct.ThrowIfCancellationRequested();

                // report owner that i am ready
                while (!ReportGuestReady(publickey, password))
                {
                    // check owner answer with small delay
                    Thread.Sleep(500);
                    ct.ThrowIfCancellationRequested();
                }

                // get my public iep

                // Change Connection state
                ConnectionEstablishingState = ConnectionEstablishingState.GettingMyPublicIp;
                // local iep.port for next use
                int localPort;
                // get my public iep and local port
                IPEndPoint myPublicIep = GetMyIEP(ct, out localPort);

                // get opponent's ip

                // Change Connection state
                ConnectionEstablishingState = ConnectionEstablishingState.WaitingForOpponentsIp;
                
                // report my public iep and get opponent's public iep
                string opponentIepString = ReportLobbyGuestIEP(publickey, password, myPublicIep);
                IPEndPoint opponentIep = opponentIepString?.ToIpEndPoint();
                // check opponent's public iep is not null as server reported that owner has reported its iep
                if (opponentIep == null)
                    throw _formatException;

                ct.ThrowIfCancellationRequested();

                // establish connection
                ConnectionEstablishingState = ConnectionEstablishingState.TryingToConnectP2P;
                return EstablishConnection(localPort, opponentIep, ct);
            }
            finally { ConnectionEstablishingState = ConnectionEstablishingState.Ready; }
        }

        #endregion

        #region Logic of controlling just created lobby

        // check lobby until opponent come and then connect him
        protected NetClientAndListener ControlMyLobby(Guid privatekey, CancellationToken ct)
        {
            ct.Register(() => _request?.Abort());
            // delete lobby on any error
            try
            {
                ct.ThrowIfCancellationRequested();
                ConnectionEstablishingState = ConnectionEstablishingState.WaitingForOpponent;
                // wait for opponent
                while (!CheckMyLobby_IsOpponentReady(privatekey))
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(RequesInterval);
                }

                // get my public iep

                // Change Connection state
                ConnectionEstablishingState = ConnectionEstablishingState.GettingMyPublicIp;
                // local iep.port for next use
                int localPort;
                // get my public iep and local port
                IPEndPoint myPublicIep = GetMyIEP(ct, out localPort);

                // get opponent's ip

                // Change Connection state
                ConnectionEstablishingState = ConnectionEstablishingState.WaitingForOpponentsIp;

                // loop until opponent reports its public iep
                string opponentIepString;
                while (true)
                {
                    // get my iep, report it to server and try get opponent's iep
                    opponentIepString = ReportLobbyOwnerIEP(privatekey, myPublicIep);
                    // if got opponent's iep
                    if (opponentIepString != null)
                        break;
                    // wait a small delay
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(500);
                }

                // try get IPEndPoint. if any format error, throw exception
                IPEndPoint opponentIep = opponentIepString.ToIpEndPoint();
                if (opponentIep == null)
                    throw _formatException;

                // establish connection
                ConnectionEstablishingState = ConnectionEstablishingState.TryingToConnectP2P;
                return EstablishConnection(localPort, opponentIep, ct);
            }
            finally
            {
                try
                {
                    MakeRequest("/api/lobby/delete/" + privatekey, "DELETE", null);
                }
                catch (Exception) { /*ignored*/ }
            }
        }

        #endregion

        #region Protected methods to communicate with server

        // check my lobby if opponent has arrived
        protected bool CheckMyLobby_IsOpponentReady(Guid privatekey)
        {
            // make request to server
            var response = MakeRequest(@"/api/lobby/checkMyLobby/" + privatekey, "GET", null);
            if (response == null)
                throw _formatException;
            // try get data from response
            bool opponentReady;
            try
            {
                opponentReady = response.guestReady;
            }
            catch (RuntimeBinderException)
            {
                throw _formatException;
            }
            return opponentReady;
        }

        // report owner iep and return guest iep or null
        protected string ReportLobbyOwnerIEP(Guid privatekey, IPEndPoint publicIep)
        {
            // encode owner iep
            string content = JsonConvert.SerializeObject(new { ownerIEP = publicIep.ToString() });
            // report it to server
            var response = MakeRequest(@"/api/lobby/reportOwnerIEP/" + privatekey, "PUT", content);
            // check response
            if (response == null)
                throw _formatException;
            // return guestIEP or null
            return response.guestIEP;
        }

        // report guest iep and return owner iep
        protected string ReportLobbyGuestIEP(int publickey, int password, IPEndPoint publicIep)
        {
            // encode guest iep
            string content = JsonConvert.SerializeObject(new { guestIEP = publicIep.ToString() });
            // report it to server
            var response = MakeRequest($@"/api/lobby/reportGuestIEP/?publickey={publickey}&password={password}",
                "PUT", content);
            return response?.ownerIEP;
        }

        // report guest is ready and return if owner has reported its iep
        protected bool ReportGuestReady(int publickey, int password)
        {
            // report i am ready and get response
            dynamic response = MakeRequest($@"/api/lobby/reportGuestReady/?publickey={publickey}&password={password}",
                "PUT", null);

            // check response format
            if (response == null)
                throw _formatException;
            try
            {// check if owner has reported its iep
                return response.ownerReportedIEP;
            }
            catch (RuntimeBinderException)
            {
                throw _formatException;
            }
        }

        #endregion

        #region Establishing connection

        // establish connection from socket on localPort to enemy on enemyIep
        protected NetClientAndListener EstablishConnection(int localPort, IPEndPoint enemyIep, CancellationToken ct)
        {
            // create listener and client
            EventBasedNetListener listener = new EventBasedNetListener();
            NetClient client = new NetClient(listener, "Battleship")
            { PeerToPeerMode = true };

            // start client on myiep
            client.Start(localPort);

            // connect to client on enemyIep
            client.Connect(enemyIep.Address.ToString(), enemyIep.Port);

            // wait for connection
            while (!client.IsConnected)
            {
                Thread.Sleep(500);
                ct.ThrowIfCancellationRequested();
            }

            return new NetClientAndListener(client, listener);
        }

        #endregion

        #region Utils

        // make request to the server and return response content
        // return string.empty, if NoContent
        // throws AggregateException (on multiple requests), TimeoutException, 
        // ArgumentException (bad _serverAdress), OperationCancelledException, 
        // DirectoryNotFoundException (bad relative url), 
        // AuthentificationException (bad password), FormatException(bad format of server response)
        protected dynamic MakeRequest(string relativeUri, string method, string content)
        {
            _request = WebRequest.CreateHttp(ServerAdress + relativeUri);
            _request.Method = method;
            _request.Accept = "application/json";
            _request.ContentType = "application/json";
            _request.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            _request.Headers.Add(HttpRequestHeader.AcceptEncoding, "utf-8");
            _request.Headers.Add(HttpRequestHeader.ContentEncoding, "utf-8");
            _request.Timeout = 30000;

            // write content to request stream
            if (string.IsNullOrWhiteSpace(content)) // if no content
            {
                // if method is neither GET nor HEAD, set ContentLength to 0
                // else - do nothing
                if (method != "GET" && method != "HEAD")
                    _request.ContentLength = 0;
            }
            else
            {
                // convert content to byte array
                byte[] contentArr = Encoding.UTF8.GetBytes(content);

                // write content to stream
                _request.ContentLength = contentArr.Length;
                using (var stream = _request.GetRequestStream())
                {
                    stream.Write(contentArr, 0, contentArr.Length);
                }
            }

            // try get response
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse) _request.GetResponse();
            }

            // rethrow webexception as specific exception

            // timeout
            catch (WebException e) when (e.Status == WebExceptionStatus.Timeout)
            {
                throw new TimeoutException("Server has not responded in defined timeout");
            }

            // server is unavailable
            catch (WebException e) when (e.Status == WebExceptionStatus.ConnectFailure)
            {
                throw new ArgumentException("Server with url from ServerInfo.json is unavailable");
            }

            // handle exception by request cancellation
            catch (WebException e) when (e.Status == WebExceptionStatus.RequestCanceled)
            {
                throw new OperationCanceledException("Request cancelled by user");
            }
            catch (WebException e)
                when ( // if server return notfound, predefined urls 
                       //in this class are not supported by the server
                    e.Status == WebExceptionStatus.ProtocolError &&
                    HttpStatusCode.NotFound.Equals((e.Response as HttpWebResponse)?.StatusCode))
            {
                throw new DirectoryNotFoundException("Server does not have predefined api. Check server version");
            }

            catch (WebException e)
                when ( // server return badrequest on bad id/password
                    e.Status == WebExceptionStatus.ProtocolError &&
                    HttpStatusCode.BadRequest.Equals((e.Response as HttpWebResponse)?.StatusCode))
            {
                throw new AuthenticationException("Lobby with defined id and password not found");
            }

            finally
            { // tell the system that request handling has ended
                _request = null;
            }

            // if no content, return string empty
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            // try decode content
            try
            {
                string text;
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    text = reader.ReadToEnd();
                }
                return JsonConvert.DeserializeObject(text);
            }
            catch (Exception e) when (e is ArgumentException || e is JsonException || e is IOException)
            {
                // if response stream can not be read or its content is not json-serialized object
                throw _formatException;
            }
            finally
            {
                response.Close();
            }
        }

        // return public endpoint of socket in the localIep
        protected IPEndPoint GetMyIEP(CancellationToken ct, out int localPort)
        {
            // create socket to find its public iep
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.ExclusiveAddressUse = false;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // randomize localport while socket can not bind the localPort
                Random rnd = new Random();
                do
                {
                    // randomize localPort
                    localPort = rnd.Next(8000, 8500);
                    // try bind
                    try
                    {
                        socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
                    } // if can not bind - indicate the error 
                    catch (Exception e) when (e is SocketException || e is SecurityException)
                    {
                        localPort = -1;
                    }
                } while (localPort < 0);


                // try get iep from _goodStunServer
                IPEndPoint result = null;

                // if working server is found
                if (_goodStunServer != null)
                {
                    result = LumiSoft_edited.STUN_Client.GetPublicEP(_goodStunServer.Item1, _goodStunServer.Item2, socket);
                    if (result != null) // if server reported my iep
                        return result;
                    else // forget this server because it is not working
                        _goodStunServer = null;
                }

                // read list of servers from file
                var file = new StringReader(Resources.StunServers);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    // read string and parse to adress and port
                    string str = file.ReadLine();
                    if (str == null)
                        throw new AggregateException("Not found any working STUN server");
                    string[] parts = str.Split(':');
                    string site = parts[0];

                    // default port of any stun server is 3478
                    int port = 3478;
                    if (parts.Length == 2) // if line contains info about port, prefer this info
                        port = int.Parse(parts[1]);

                    // try get result
                    result = LumiSoft_edited.STUN_Client.GetPublicEP(site, port, socket);
                    if (result != null) // if server report my iep
                    {
                        _goodStunServer = Tuple.Create(site, port); // save server as working
                        break; // break loop and return result
                    }
                }
                file.Close();
                return result; 
            }
        }

        #endregion

    }
}
