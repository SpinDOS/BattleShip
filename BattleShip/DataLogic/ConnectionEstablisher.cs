using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BattleShip.DataLogic
{
    class ConnectionEstablisher
    {
        public void CreateLobby()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://localhost:3184/api/randomopponent/");
            req.Method = "Get";
            req.Accept = "application/json";
            req.ContentType = "application/json";
            req.Timeout = 30000;

            req.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            req.Headers.Add(HttpRequestHeader.AcceptEncoding, "utf-8");
            req.Headers.Add(HttpRequestHeader.ContentEncoding, "utf-8");
            //string s = JsonConvert.SerializeObject(new {info = "abcdABCS"});
            //byte[] input = Encoding.UTF8.GetBytes(s);
            //req.ContentLength = input.Length;
            //using (var stream = req.GetRequestStream())
            //{
            //    stream.Write(input, 0, input.Length);
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
