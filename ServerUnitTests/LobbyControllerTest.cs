using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShipRendezvousServer;
using BattleShipRendezvousServer.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace ServerUnitTests
{
    public class LobbyControllerTest
    {
        private TestServer _server;
        private HttpClient _client;

        public LobbyControllerTest()
        {
            _server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async void CheckLobbyController()
        {
            // create lobby
            var response = await _client.GetAsync("/api/lobby/create");
            // check ok status code
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // read data from response
            dynamic dyn = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            Guid privatekey = dyn.privateKey;
            int publickey = dyn.publicKey;
            int password = dyn.password;

            // check ability to connect to owner
            response = await _client.PutAsync
                ($"/api/lobby/ReportGuestReady/?publickey={publickey}&password={password}", new ByteArrayContent(new byte[0]));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // check for exception by bad password
            try
            {
                response = await _client.PutAsync
                ($"/api/lobby/ReportGuestReady/?publickey={publickey}&password={password + 1}",
                    new ByteArrayContent(new byte[0]));
            }
            catch (AuthenticationException) { }

            // check for error while connecting not exisiting lobby
            response = await _client.PutAsync
                ($"/api/lobby/ReportGuestReady/?publickey={publickey+1}&password={password}", new ByteArrayContent(new byte[0]));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            ConnectToMyself(privatekey, publickey, password);
        }

        [Fact]
        public async void CheckRandomOpponent()
        {
            // create first enemy
            var response = await _client.GetAsync("/api/randomopponent/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            dynamic dyn = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            // check if the entry is first of random search
            Assert.False((bool) dyn.found);
            Guid privatekey = dyn.privateKey;

            // create second enemy
            response = await _client.GetAsync("/api/randomopponent/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            dynamic dyn1 = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            // check if the entry is second of random search
            Assert.True((bool)dyn1.found);
            int publickey = dyn1.publicKey;
            int password = dyn1.password;

            ConnectToMyself(privatekey, publickey, password);

            // create first enemy
            response = await _client.GetAsync("/api/randomopponent/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            dynamic dyn2 = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            // check if the entry is first of random search
            Assert.False((bool) dyn2.found);

            // wait for first enemy entry remove by sliding expiration
            Thread.Sleep(15000);

            // create first enemy
            response = await _client.GetAsync("/api/randomopponent/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            dynamic dyn3 = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            // check if the entry is first of random search
            Assert.False((bool) dyn3.found);
        }

        private void ConnectToMyself(Guid privatekey, int publickey, int password)
        {
            // wait 2/3 of sliding expiration delay
            Thread.Sleep(10000);

            // check guest ready
            var response = _client.GetAsync($"/api/lobby/CheckMyLobby/{privatekey}").Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string str = response.Content.ReadAsStringAsync().Result;
            dynamic dyn4 = JsonConvert.DeserializeObject(str);
            Assert.True((bool) dyn4.guestReady);

            // report owner iep and refresh sliding expiration delay
            IPEndPoint iep1 = new IPEndPoint(IPAddress.Parse("192.168.240.130"), 6532);
            str = JsonConvert.SerializeObject(new {ownerIEP = iep1.ToString()});
            HttpContent content = new StringContent(str);
            content.Headers.ContentEncoding.Add("utf-8");
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            response = _client.PutAsync($"/api/lobby/ReportOwnerIEP/{privatekey}", content).Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return;


            // wait more time for sliding expiration delay refresh check
            Thread.Sleep(10000);

            // report guest iep
            IPEndPoint iep2 = new IPEndPoint(IPAddress.Parse("192.168.240.131"), 6533);
            str = JsonConvert.SerializeObject(new { guestIEP = iep2.ToString() });
            content = new StringContent(str);
            content.Headers.ContentEncoding.Add("utf-8");
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            response = _client.PutAsync($"/api/lobby/ReportGuestIEP/?publickey={publickey}&password={password}", content).Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // get owner iep
            str = response.Content.ReadAsStringAsync().Result;
            dynamic dyn5 = JsonConvert.DeserializeObject(str);
            str = dyn5.ownerIEP;
            Assert.Equal(iep1, str.ToIpEndPoint());

            // get guest iep
            str = JsonConvert.SerializeObject(new { ownerIEP = iep1.ToString() });
            content = new StringContent(str);
            content.Headers.ContentEncoding.Add("utf-8");
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            response = _client.PutAsync($"/api/lobby/ReportOwnerIEP/{privatekey}", content).Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            str = response.Content.ReadAsStringAsync().Result;
            dynamic dyn6 = JsonConvert.DeserializeObject(str);
            str = dyn6.guestIEP;
            Assert.Equal(iep2, str.ToIpEndPoint());

            // remove lobby
            response = _client.DeleteAsync($"/api/lobby/delete/{privatekey}").Result;
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

}
