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
        /// <exception cref="TimeoutException">thrown if server or opponent has not responded during timeout</exception>
        /// <exception cref="ArgumentException">thrown if server from is unavailable</exception>
        /// <exception cref="OperationCanceledException">thrown if operation cancelled by user</exception>
        /// <exception cref="DirectoryNotFoundException">thrown if pre-defined relative url is unavailable</exception>
        /// <exception cref="AuthenticationException">thrown if RequestInterval is too large 
        /// and server delete created lobby or another server authentification error</exception>
        public NetClientAndListener GetRandomOpponent(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Change Connection state
            if (ConnectionEstablishingState != ConnectionEstablishingState.Ready)
                throw new AggregateException("Opponent search is already in progress");
            ConnectionEstablishingState = ConnectionEstablishingState.GettingInfoFromServer;

            // reset ConnectionEstablishingState on exception
            try
            {
                // get response
                var response = MakeRequest(@"/api/randomOpponent", "GET", null, ct);

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
        /// <exception cref="TimeoutException">thrown if server or opponent has not responded during timeout</exception>
        /// <exception cref="ArgumentException">thrown if server from is unavailable</exception>
        /// <exception cref="OperationCanceledException">thrown if operation cancelled by user</exception>
        /// <exception cref="DirectoryNotFoundException">thrown if pre-defined relative url is unavailable</exception>
        /// <exception cref="AuthenticationException">thrown if RequestInterval is too large and server delete created lobby</exception>
        public NetClientAndListener CreateLobby(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Change Connection state
            if (ConnectionEstablishingState != ConnectionEstablishingState.Ready)
                throw new AggregateException("Opponent is already found");
            ConnectionEstablishingState = ConnectionEstablishingState.GettingInfoFromServer;

            // reset ConnectionEstablishingState on exception
            try
            {
                dynamic response = MakeRequest(@"/api/lobby/create", "GET", null, ct);
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
        /// <exception cref="TimeoutException">thrown if server or opponent has not responded during timeout</exception>
        /// <exception cref="ArgumentException">thrown if server from is unavailable</exception>
        /// <exception cref="OperationCanceledException">thrown if operation cancelled by user</exception>
        /// <exception cref="DirectoryNotFoundException">thrown if pre-defined relative url is unavailable</exception>
        /// <exception cref="AuthenticationException">thrown if publickey or password are not found in the server</exception>
        public NetClientAndListener ConnectLobby(int publickey, int password, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Change Connection state
            if (ConnectionEstablishingState != ConnectionEstablishingState.Ready)
                throw new AggregateException("Opponent is already found");
            ConnectionEstablishingState = ConnectionEstablishingState.WaitingForOpponent;

            // reset ConnectionEstablishingState on exception
            try
            {
                // report owner that i am ready
                while (!ReportGuestReady(publickey, password, ct))
                {
                    // check owner answer with small delay
                    Thread.Sleep(500);
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
                IPEndPoint opponentIep = ReportLobbyGuestIEP(publickey, password, myPublicIep, ct);

                ct.ThrowIfCancellationRequested();

                // establish connection
                return EstablishConnection(localPort, opponentIep, ct);
            }
            finally { ConnectionEstablishingState = ConnectionEstablishingState.Ready; }
        }

        #endregion

        #region Logic of controlling just created lobby

        // check lobby until opponent come and then connect him
        protected NetClientAndListener ControlMyLobby(Guid privatekey, CancellationToken ct)
        {
            // delete lobby on any error
            try
            {
                ct.ThrowIfCancellationRequested();
                ConnectionEstablishingState = ConnectionEstablishingState.WaitingForOpponent;
                // wait for opponent
                while (!CheckMyLobby_IsOpponentReady(privatekey, ct))
                {
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
                IPEndPoint opponentIep;
                DateTime waitingExpiration = DateTime.Now + TimeSpan.FromSeconds(15);
                while (true)
                {
                    // check waiting time
                    if (DateTime.Now > waitingExpiration)
                        throw new TimeoutException("Enemy reported he is ready but he is not sharing his IP");
                    // get my iep, report it to server and try get opponent's iep
                    opponentIep = ReportLobbyOwnerIEP(privatekey, myPublicIep, ct);
                    // if got opponent's iep
                    if (opponentIep != null)
                        break;
                    // wait a small delay
                    Thread.Sleep(500);
                }

                // establish connection
                return EstablishConnection(localPort, opponentIep, ct);
            }
            finally
            {
                ThreadPool.QueueUserWorkItem(o => MakeRequest("/api/lobby/delete/" + privatekey, "DELETE", null,
                    CancellationToken.None));
            }
        }

        #endregion

        #region Protected methods to communicate with server

        // check my lobby if opponent has arrived
        protected bool CheckMyLobby_IsOpponentReady(Guid privatekey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // make request to server
            var response = MakeRequest(@"/api/lobby/checkMyLobby/" + privatekey, "GET", null, ct);
            // try get data from response
            try
            {
                if (response != null)
                    return (bool) response.guestReady;
            }
            catch (RuntimeBinderException) { }
            throw _formatException;
        }

        // report owner iep and return guest iep or null
        protected IPEndPoint ReportLobbyOwnerIEP(Guid privatekey, IPEndPoint publicIep, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // encode owner iep
            string content = JsonConvert.SerializeObject(new { ownerIEP = publicIep.ToString() });
            // report it to server
            var response = MakeRequest(@"/api/lobby/reportOwnerIEP/" + privatekey, "PUT", content, ct);
            // check response
            if (response == null)
                throw _formatException;
            // parse response
            string str = response.guestIEP;
            IPEndPoint iep = str.ToIpEndPoint();
            // if got data but its not IpEndPoint
            if (str != null && iep == null)
                throw _formatException;
            return iep;
        }

        // report guest iep and return owner iep
        protected IPEndPoint ReportLobbyGuestIEP(int publickey, int password, IPEndPoint publicIep, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // encode guest iep
            string content = JsonConvert.SerializeObject(new { guestIEP = publicIep.ToString() });
            // report it to server
            var response = MakeRequest($@"/api/lobby/reportGuestIEP/?publickey={publickey}&password={password}",
                "PUT", content, ct);
            // parse response
            // check opponent's public iep is not null as server reported that owner has reported its iep
            string str = response?.ownerIEP;
            var iep = str.ToIpEndPoint();
            if (iep == null)
                throw _formatException;
            return iep;
        }

        // report guest is ready and return if owner has reported its iep
        protected bool ReportGuestReady(int publickey, int password, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // report i am ready and get response
            dynamic response = MakeRequest($@"/api/lobby/reportGuestReady/?publickey={publickey}&password={password}",
                "PUT", null, ct);
            try
            {
                // check if owner has reported its iep
                if (response != null)
                    return response.ownerReportedIEP;
            }
            catch (RuntimeBinderException) { }
            throw _formatException;
        }

        #endregion

        #region Establishing connection

        // establish connection from socket on localPort to enemy on enemyIep
        protected NetClientAndListener EstablishConnection(int localPort, IPEndPoint enemyIep, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ConnectionEstablishingState = ConnectionEstablishingState.TryingToConnectP2P;
            // create listener and client
            EventBasedNetListener listener = new EventBasedNetListener();
            NetClient client = new NetClient(listener, "Battleship")
            { PeerToPeerMode = true };

            // start client on myiep
            client.Start(localPort);

            // connect to client on enemyIep
            client.Connect(enemyIep.Address.ToString(), enemyIep.Port);

            DateTime waitingExpiration = DateTime.Now + TimeSpan.FromSeconds(15);
            // wait for connection
            while (!client.IsConnected)
            {
                // check connection time
                if (DateTime.Now > waitingExpiration)
                {
                    client.Stop();
                    throw new TimeoutException("Can not connect enemy");
                }
                Thread.Sleep(500);
                ct.ThrowIfCancellationRequested();
            }

            return new NetClientAndListener(client, listener);
        }

        #endregion

        #region Utils

        // make request to the server and return response content
        // return null, if NoContent
        // throws AggregateException (on multiple requests), TimeoutException, 
        // ArgumentException (bad _serverAdress), OperationCancelledException, 
        // DirectoryNotFoundException (bad relative url), 
        // AuthentificationException (bad password), FormatException(bad format of server response)
        protected dynamic MakeRequest(string relativeUri, string method, string content, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var request = WebRequest.CreateHttp(ServerAdress + relativeUri);
            request.Method = method;
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "utf-8");
            request.Headers.Add(HttpRequestHeader.ContentEncoding, "utf-8");
            request.Timeout = 15000;

            // write content to request stream
            if (string.IsNullOrWhiteSpace(content)) // if no content
            {
                // if method is neither GET nor HEAD, set ContentLength to 0
                // else - do nothing
                if (method != "GET" && method != "HEAD")
                    request.ContentLength = 0;
            }
            else
            {
                // convert content to byte array
                byte[] contentArr = Encoding.UTF8.GetBytes(content);

                // write content to stream
                request.ContentLength = contentArr.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(contentArr, 0, contentArr.Length);
                }
            }

            // try get response and rethrow exceptions
            try
            {
                using (ct.Register(() => request.Abort()))
                {
                    using (var response = (HttpWebResponse) request.GetResponse())
                    {
                        // if no content, return null
                        if (response.StatusCode == HttpStatusCode.NoContent)
                            return null;

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
                    }
                }
            }

            // rethrow webexception as specific exception

            // timeout
            catch (WebException e) when (e.Status == WebExceptionStatus.Timeout)
            {
                throw new TimeoutException("Server has not responded in defined timeout", e);
            }

            // server is unavailable
            catch (WebException e) when (e.Status == WebExceptionStatus.ConnectFailure)
            {
                throw new ArgumentException("Server with defined url is unavailable", e);
            }

            // handle exception by request cancellation
            catch (WebException e) when (e.Status == WebExceptionStatus.RequestCanceled)
            {
                throw new OperationCanceledException("Request cancelled by user", e);
            }
            catch (WebException e)
                when ( // if server return notfound, predefined urls 
                    //in this class are not supported by the server
                    e.Status == WebExceptionStatus.ProtocolError &&
                    HttpStatusCode.NotFound.Equals((e.Response as HttpWebResponse)?.StatusCode))
            {
                throw new DirectoryNotFoundException("Server does not have predefined api. Check server version", e);
            }

            catch (WebException e)
                when ( // server return badrequest on bad id/password
                    e.Status == WebExceptionStatus.ProtocolError &&
                    HttpStatusCode.BadRequest.Equals((e.Response as HttpWebResponse)?.StatusCode))
            {
                throw new AuthenticationException("Lobby with defined id and password not found", e);
            }
        }

        // return public endpoint of socket in the localIep
        protected IPEndPoint GetMyIEP(CancellationToken ct, out int localPort)
        {
            ct.ThrowIfCancellationRequested();
            // create socket to find its public iep
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.ExclusiveAddressUse = false;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // try get iep from _goodStunServer
                IPEndPoint result = null;

                // if working server is found
                if (_goodStunServer != null)
                {
                    result = LumiSoft_edited.STUN_Client.GetPublicEP(_goodStunServer.Item1, _goodStunServer.Item2, socket);
                    if (result != null) // if server reported my iep
                    {
                        localPort = (socket.LocalEndPoint as IPEndPoint).Port;
                        return result;
                    }
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
                localPort = (socket.LocalEndPoint as IPEndPoint).Port;
                return result; 
            }
        }

        #endregion

    }
}
