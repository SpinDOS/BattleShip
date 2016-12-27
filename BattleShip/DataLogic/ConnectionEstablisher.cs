using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace BattleShip.DataLogic
{
    class ConnectionEstablisher
    {
        public void CreateLobby()
        {
            Guid guid = Guid.NewGuid();
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("192.168.100.130"), 45030);
            string s = JsonConvert.SerializeObject(new {OwnerIEP = iep.ToString()});
            byte[] arrrr = Encoding.UTF8.GetBytes(s);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://localhost:3184/api/lobby/ReportGuestReady/?publickey=3&password=4");
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
            //req.ContentLength = arrrr.Length;
            //using (var stream = req.GetRequestStream())
            //{
            //    stream.Write(arrrr, 0, arrrr.Length);
            //    stream.Flush();
            //    stream.Close();
            //}
            var res = req.GetResponse() as HttpWebResponse;
            
            var stream1 = res.GetResponseStream();
            byte[] arr = new byte[10000];
            int i = 0;
            var count = stream1.ReadAsync(arr, 0, arr.Length).Result;
            string str = Encoding.UTF8.GetString(arr, 0, count);
            var dt = JsonConvert.DeserializeObject<DateTime>(str);

        }
    }
}
