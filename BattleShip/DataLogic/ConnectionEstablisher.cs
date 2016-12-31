using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace BattleShip.DataLogic
{
    class ConnectionEstablisher
    {
        private string _serverAdress;

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

        public ConnectionState ConnectionState
        {
            get { return _connectionState; }
            protected set { _connectionState = value; }
        }

        public void GetRandomOpponent()
        {

        }

        public void CreateLobby()
        {
            var xsfds = MakeRequest("/api/lobby/delete/" + Guid.NewGuid().ToString() + "?publickey=32&password=32423", "DELETE", null);
            Guid guid = Guid.NewGuid();
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("192.168.100.130"), 45030);
            string s = JsonConvert.SerializeObject(new {OwnerIEP = iep.ToString()});
            byte[] arrrr = Encoding.UTF8.GetBytes(s);
            HttpWebRequest req =
                (HttpWebRequest) WebRequest.Create("http://localhost:3184/api/lobby/ReportOwnerIEP/" + guid.ToString());
            req.Method = "PUT";
            req.Accept = "application/json";
            req.ContentType = "application/json";
            req.Timeout = 30000;

            req.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            req.Headers.Add(HttpRequestHeader.AcceptEncoding, "utf-8");
            req.Headers.Add(HttpRequestHeader.ContentEncoding, "utf-8");
            //string s = JsonConvert.SerializeObject(new {info = "abcdABCS"});
            //byte[] input = Encoding.UTF8.GetBytes(s);
            req.ContentLength = 0;
            req.ContentLength = arrrr.Length;
            using (var stream = req.GetRequestStream())
            {
                stream.Write(arrrr, 0, arrrr.Length);
                stream.Flush();
                stream.Close();
            }
            var res = req.GetResponse() as HttpWebResponse;

            var stream1 = res.GetResponseStream();
            byte[] arr = new byte[10000];
            int i = 0;
            var count = stream1.ReadAsync(arr, 0, arr.Length).Result;
            string str = Encoding.UTF8.GetString(arr, 0, count);
            var dt = JsonConvert.DeserializeObject<DateTime>(str);

        }

        private string MakeRequest(string relativeUri, string method, string content)
        {
            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(_serverAdress + relativeUri);
            req.Method = method;
            req.Accept = "application/json";
            req.ContentType = "application/json";
            req.Timeout = 30000;
            req.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            req.Headers.Add(HttpRequestHeader.AcceptEncoding, "utf-8");
            req.Headers.Add(HttpRequestHeader.ContentEncoding, "utf-8");
            if (content == null)
            {
                if (method != "GET" && method != "HEAD")
                    req.ContentLength = 0;
            }
            else
            {
                byte[] contentArr = Encoding.UTF8.GetBytes(content);
                req.ContentLength = contentArr.Length;
                using (var stream = req.GetRequestStream())
                {
                    stream.Write(contentArr, 0, contentArr.Length);
                }
            }
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse) req.GetResponse();
            }
            catch (WebException e) when (e.Status == WebExceptionStatus.Timeout)
            {
                return "timeout";
            }
            catch (WebException e) when (e.Status == WebExceptionStatus.ConnectFailure)
            {
                return "server unavailable";
            }
            catch (WebException e) when (e.Status == WebExceptionStatus.RequestCanceled)
            {
                return "Cancelled by user";
            }
            catch (WebException e)
                when (
                    e.Status == WebExceptionStatus.ProtocolError &&
                    (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
            {
                return "Not found";
            }
            catch (WebException e)
                when (
                    e.Status == WebExceptionStatus.ProtocolError &&
                    (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.BadRequest)
            {
                return "bad login";
            }
            if (response.ContentLength == -1)
            {
                return "no content";
            }
            byte[] output = new byte[response.ContentLength];
            using (var stream = response.GetResponseStream())
            {
                stream.Read(output, 0, output.Length);
            }
            return Encoding.UTF8.GetString(output);
        }
    }
}
