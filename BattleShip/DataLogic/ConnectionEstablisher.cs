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
    class ConnectionEstablisher
    {
        protected string _serverAdress;

        public ConnectionEstablisher()
        {
            string text;
            try
            {
                text = File.ReadAllText("ServerInfo.json");
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException || e is IOException ||
                    e is UnauthorizedAccessException || e is SecurityException)
                    throw new FileLoadException("Could not load data from ServerInfo.json");
                throw;
            }
            dynamic dyn = JsonConvert.DeserializeObject(text);
            try
            {
                _serverAdress = dyn.ServerAdress;
            }
            catch (RuntimeBinderException)
            {
                _serverAdress = null;
            }
            if (string.IsNullOrWhiteSpace(_serverAdress))
                throw new InvalidOperationException("Could not find server adress in ServerInfo.json");
        }

        private volatile ConnectionState _connectionState = DataLogic.ConnectionState.Disconnected;

        /// <summary>
        /// Return state of finding opponent process
        /// </summary>
        public ConnectionState ConnectionState
        {
            get { return _connectionState; }
            protected set { _connectionState = value; }
        }

        // volatile for multi-thread cancellation
        private volatile HttpWebRequest _request;

        // exception for any server response format error
        private FormatException _formatException = new FormatException("Bad format of server response");

        public void GetRandomOpponent(CancellationToken ct)
        {
            // Change Connection state
            if (ConnectionState != ConnectionState.Disconnected)
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
                catch (RuntimeBinderException e)
                {
                    throw _formatException;
                }

                ConnectLobby(publickey, password, ct);
            }
            else
            {
                // check format
                Guid privatekey;
                try
                {
                    privatekey = response.privateKey;
                }
                catch (RuntimeBinderException e)
                {
                    throw _formatException;
                }

                ControlMyLobby(privatekey, ct);
            }
        }

        public TimeSpan RequesInterval { get; set; } = TimeSpan.FromSeconds(10);

        private void ControlMyLobby(Guid privatekey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ConnectionState = ConnectionState.WaitingForOpponent;
            while (!CheckMyLobby_IsOpponentReady(privatekey))
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(RequesInterval);
            }

            IPEndPoint localIep = new IPEndPoint(IPAddress.Any, new Random().Next(8000, 8500));
            IPEndPoint myPublicIep = GetMyIEP(localIep, ct);
            string opponentIepString;
            while ((opponentIepString = ReportLobbyOwnerIEP(privatekey, myPublicIep)) == null)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(500);
            }
            IPEndPoint opponentIep = opponentIepString.ToIpEndPoint();
            EstablishConnection(localIep, opponentIep);
            MakeRequest("/api/lobby/delete/" + privatekey, "DELETE", null);
        }

        private void ConnectLobby(int publickey, int password, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ConnectionState = ConnectionState.WaitingForOpponentsIP;
            MakeRequest($@"/api/lobby/reportGuestReady/?publickey={publickey}&password={password}",
                "PUT", null);
            ct.ThrowIfCancellationRequested();
            IPEndPoint localIep = new IPEndPoint(IPAddress.Any, new Random().Next(8000, 8500));
            IPEndPoint myPublicIep = GetMyIEP(localIep, ct);
            string opponentIepString;
            while ((opponentIepString = ReportLobbyGuestIEP(publickey, password, myPublicIep)) == null)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(500);
            }
            IPEndPoint opponentIep = opponentIepString.ToIpEndPoint();
            EstablishConnection(localIep, opponentIep);
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
            if (_request != null)
                throw new AggregateException("Previous request has not ended");
            // set up request 
            _request = (HttpWebRequest) WebRequest.Create(_serverAdress + relativeUri);
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
                    (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
            {
                throw new DirectoryNotFoundException("Server does not have predefined api. Check server version");
            }

            catch (WebException e)
                when ( // server return badrequest on bad id/password
                    e.Status == WebExceptionStatus.ProtocolError &&
                    (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.BadRequest)
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

        private IPEndPoint GetMyIEP(IPEndPoint localIep, CancellationToken ct)
        {
            var file = new StringReader(Resources.StunServers);
            IPEndPoint result = null;

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

                result = LumiSoft_edited.STUN_Client.GetPublicEP(site, port, localIep);
            }
            file.Close();
            return result;
        }

        private bool CheckMyLobby_IsOpponentReady(Guid privatekey)
        {
            var response = MakeRequest(@"/api/lobby/checkMyLobby/" + privatekey , "GET", null);
            if (response == null)
                throw _formatException;
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

        private string ReportLobbyOwnerIEP(Guid privatekey, IPEndPoint publicIep)
        {
            string content = JsonConvert.SerializeObject(new {ownerIEP = publicIep.ToString()});
            var response = MakeRequest(@"/api/lobby/reportOwnerIEP/" + privatekey, "PUT", content);
            if (response == null)
                throw _formatException;
            return response.guestIEP;
        }
        private string ReportLobbyGuestIEP(int publickey, int password, IPEndPoint publicIep)
        {
            string content = JsonConvert.SerializeObject(new { guestIEP = publicIep.ToString()});
            var response = MakeRequest($@"/api/lobby/reportGuestIEP/?publickey={publickey}&password={password}", 
                "PUT", content);
            if (response == null)
                throw _formatException;
            return response.ownerIEP;
        }

        private void EstablishConnection(IPEndPoint myiep, IPEndPoint enemyIep)
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
            
            //Thread.Sleep(1000);
            //client.PollEvents();
        }
    }
}
