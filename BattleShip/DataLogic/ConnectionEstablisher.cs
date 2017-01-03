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
        // adress of the server
        protected string _serverAdress;

        /// <summary>
        /// Initialize class with server adress from ServerInfo.json and check connection to the server
        /// </summary>
        /// <exception cref="FileLoadException">Can not read data from ServerInfo.json</exception>
        /// <exception cref="InvalidOperationException">ServerInfo.json does not contain adress of valid server</exception>
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
            _serverAdress = dyn?.ServerAdress;

            // if not found serveradress
            if (string.IsNullOrWhiteSpace(_serverAdress))
                throw new InvalidOperationException("Could not find server adress in ServerInfo.json");

            // try connect to server
            try
            {
                var response = WebRequest.Create(_serverAdress).GetResponse();
                response.Close();
            }
            // if can not connect to server
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ConnectFailure)
                    throw new InvalidOperationException("Could not connect server from the ServerInfo.json");
                // if any other status, server is ok => ignore exception
            }
            // if bad uri 
            catch (Exception e) when (e is NotSupportedException || e is UriFormatException)
            {
                throw new InvalidOperationException("Invalid server adress in the ServerInfo.json");
            }
        }

        private volatile ConnectionState _connectionState = DataLogic.ConnectionState.Ready;

        /// <summary>
        /// Return state of finding opponent process
        /// </summary>
        public ConnectionState ConnectionState
        {
            get { return _connectionState; }
            protected set { _connectionState = value; }
        }

        // volatile for multi-thread cancellation
        protected volatile HttpWebRequest _request;

        // exception for any server response format error
        private readonly FormatException _formatException = new FormatException("Bad format of server response");

        /// <summary>
        /// Gets random opponent
        /// </summary>
        /// <param name="ct">token to cancel process</param>
        /// <returns>netclient connected to found opponent</returns>
        public NetClient GetRandomOpponent(CancellationToken ct)
        {
            // Change Connection state
            if (ConnectionState != ConnectionState.Ready)
                throw new AggregateException("Opponent is already found");
            ConnectionState = ConnectionState.GettingInfoFromServer;

            // abort request on cancellation
            ct.Register(() => _request?.Abort());

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
                ConnectionState = ConnectionState.Ready;
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

        /// <summary>
        /// Interval between requests to server to check if opponent found
        /// </summary>
        public TimeSpan RequesInterval { get; set; } = TimeSpan.FromSeconds(7.5);

        // check lobby until opponent come and then connect him
        protected NetClient ControlMyLobby(Guid privatekey, CancellationToken ct)
        {
            ct.Register(() => _request?.Abort());
            // delete lobby on any error
            try
            {
                ct.ThrowIfCancellationRequested();
                ConnectionState = ConnectionState.WaitingForOpponent;
                // wait for opponent
                while (!CheckMyLobby_IsOpponentReady(privatekey))
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(RequesInterval);
                }

                // create local iep
                IPEndPoint localIep = new IPEndPoint(IPAddress.Any, new Random().Next(8000, 8500));

                // change ConnectionState
                ConnectionState = ConnectionState.WaitingForOpponentsIP;

                // loop until opponent reports its public iep
                string opponentIepString;
                while (true)
                {
                    // get my iep, report it to server and try get opponent's iep
                    opponentIepString = ReportLobbyOwnerIEP(privatekey, GetMyIEP(localIep, ct));
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
                ConnectionState = ConnectionState.TryingToConnectP2P;
                return EstablishConnection(localIep, opponentIep, ct);
            }
            finally
            {
                ConnectionState = ConnectionState.Ready;
                try
                {
                    MakeRequest("/api/lobby/delete/" + privatekey, "DELETE", null);
                }
                catch (Exception) { /*ignored*/ }
            }
        }

        public NetClient ConnectLobby(int publickey, int password, CancellationToken ct)
        {
            // Change Connection state
            if (ConnectionState != ConnectionState.Ready)
                throw new AggregateException("Opponent is already found");
            ConnectionState = ConnectionState.WaitingForOpponent;

            ct.ThrowIfCancellationRequested();

            // report owner that i am ready
            while (!ReportGuestReady(publickey, password))
            {
                // check owner answer with small delay
                Thread.Sleep(500);
                ct.ThrowIfCancellationRequested();
            }

            // Change Connection state
            ConnectionState = ConnectionState.WaitingForOpponentsIP;

            // create local iep
            IPEndPoint localIep = new IPEndPoint(IPAddress.Any, new Random().Next(8000, 8500));
            // get my public iep
            IPEndPoint myPublicIep = GetMyIEP(localIep, ct);
            // report my public iep and get opponent's public iep
            string opponentIepString = ReportLobbyGuestIEP(publickey, password, myPublicIep);
            IPEndPoint opponentIep = opponentIepString?.ToIpEndPoint();
            // check opponent's public iep is not null as server reported that owner has reported its iep
            if (opponentIep == null)
                throw _formatException;

            // establish connection
            ConnectionState = ConnectionState.TryingToConnectP2P;
            return EstablishConnection(localIep, opponentIep, ct);
        }

        public void CreateLobby()
        {
            ////var xsfds = MakeRequest("/api/lobby/delete/" + Guid.NewGuid().ToString() + "?publickey=32&password=32423", "DELETE", null);
            //Guid guid = Guid.NewGuid();
            //IPEndPoint iep = new IPEndPoint(IPAddress.Parse("192.168.100.130"), 45030);
            //string s = JsonConvert.SerializeObject(new {OwnerIEP = iep.ToString()});
            //byte[] arrrr = Encoding.UTF8.GetBytes(s);
            //HttpWebRequest req =
            //    (HttpWebRequest) WebRequest.Create("http://localhost:3184/api/lobby/ReportOwnerIEP/" + guid.ToString());
            //req.Method = "PUT";
            //req.Accept = "application/json";
            //req.ContentType = "application/json";
            //req.Timeout = 30000;

            //req.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            //req.Headers.Add(HttpRequestHeader.AcceptEncoding, "utf-8");
            //req.Headers.Add(HttpRequestHeader.ContentEncoding, "utf-8");
            ////string s = JsonConvert.SerializeObject(new {info = "abcdABCS"});
            ////byte[] input = Encoding.UTF8.GetBytes(s);
            //req.ContentLength = 0;
            //req.ContentLength = arrrr.Length;
            //using (var stream = req.GetRequestStream())
            //{
            //    stream.Write(arrrr, 0, arrrr.Length);
            //    stream.Flush();
            //    stream.Close();
            //}
            //var res = req.GetResponse() as HttpWebResponse;

            //var stream1 = res.GetResponseStream();
            //byte[] arr = new byte[10000];
            //int i = 0;
            //var count = stream1.ReadAsync(arr, 0, arr.Length).Result;
            //string str = Encoding.UTF8.GetString(arr, 0, count);
            //var dt = JsonConvert.DeserializeObject<DateTime>(str);

        }

        // bufer for decoding responses
        private byte[] _bufer = new byte[200];

        // make request to the server and return response content
        // return string.empty, if NoContent
        // throws AggregateException (on multiple requests), TimeoutException, 
        // ArgumentException (bad _serverAdress), OperationCancelledException, 
        // DirectoryNotFoundException (bad relative url), 
        // AuthentificationException (bad password), FormatException(bad format of server response)
        private dynamic MakeRequest(string relativeUri, string method, string content)
        {
            _request = WebRequest.CreateHttp(_serverAdress + relativeUri);
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

            // read content
            int count;
            using (var stream = response.GetResponseStream())
            {
                count = stream.Read(_bufer, 0, _bufer.Length);
            }
            response.Close();

            // try decode content
            try
            {
                return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(_bufer, 0, count));
            }
            catch (Exception e)
            { // if byte array can not be converted to string or string is not json-serialized object
                if (e is ArgumentException || e is JsonException)
                    throw _formatException;
                throw;
            }
        }

        private Tuple<string, int> _goodStunServer = null;
        private IPEndPoint GetMyIEP(IPEndPoint localIep, CancellationToken ct)
        {
            IPEndPoint result = null;
            if (_goodStunServer != null)
            {
                result = LumiSoft_edited.STUN_Client.GetPublicEP(_goodStunServer.Item1, _goodStunServer.Item2, localIep);
                if (result != null)
                    return result;
            }

            var file = new StringReader(Resources.StunServers);

            while (result == null)
            {
                ct.ThrowIfCancellationRequested();
                string str = file.ReadLine();
                if (str == null)
                    throw new AggregateException("Not found any working STUN server");
                string[] parts = str.Split(':');
                string site = parts[0];
                int port = 3478;
                if (parts.Length == 2)
                    port = int.Parse(parts[1]);

                _goodStunServer = Tuple.Create(site, port);
                result = LumiSoft_edited.STUN_Client.GetPublicEP(site, port, localIep);
            }
            file.Close();
            return result;
        }

        // check my lobby if opponent has arrived
        private bool CheckMyLobby_IsOpponentReady(Guid privatekey)
        {
            // make request to server
            var response = MakeRequest(@"/api/lobby/checkMyLobby/" + privatekey , "GET", null);
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
        private string ReportLobbyOwnerIEP(Guid privatekey, IPEndPoint publicIep)
        {
            // encode owner iep
            string content = JsonConvert.SerializeObject(new {ownerIEP = publicIep.ToString()});
            // report it to server
            var response = MakeRequest(@"/api/lobby/reportOwnerIEP/" + privatekey, "PUT", content);
            // check response
            if (response == null)
                throw _formatException;
            // return guestIEP or null
            return response.guestIEP;
        }

        // report guest iep and return owner iep
        private string ReportLobbyGuestIEP(int publickey, int password, IPEndPoint publicIep)
        {
            // encode guest iep
            string content = JsonConvert.SerializeObject(new { guestIEP = publicIep.ToString()});
            // report it to server
            var response = MakeRequest($@"/api/lobby/reportGuestIEP/?publickey={publickey}&password={password}", 
                "PUT", content);
            return response?.ownerIEP;
        }

        // report guest is ready and return if owner has reported its iep
        private bool ReportGuestReady(int publickey, int password)
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

        private NetClient EstablishConnection(IPEndPoint myiep, IPEndPoint enemyIep, CancellationToken ct)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            listener.PeerConnectedEvent += peer => MessageBox.Show("peer connected " + peer.EndPoint);
            listener.NetworkReceiveEvent += (peer, reader) => MessageBox.Show("Message received");
            NetClient client = new NetClient(listener, "Battleship");
            client.PeerToPeerMode = true;
            client.Start(myiep.Port);
            client.Connect(enemyIep.Address.ToString(), enemyIep.Port);
            while (!client.IsConnected)
            {
                Thread.Sleep(500);
            }
            client.PollEvents();
            client.Peer.Send(new byte[10], SendOptions.ReliableOrdered);
            Thread.Sleep(5000);
            client.PollEvents();
            MessageBox.Show("OK");
            return client;
        }
    }
}
