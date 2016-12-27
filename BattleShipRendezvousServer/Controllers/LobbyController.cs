using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShipRendezvousServer.Dependency_Injection;
using BattleShipRendezvousServer.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace BattleShipRendezvousServer.Controllers
{
    /* TODO 
     * CheckMyLobby
     * ReportOwnerIEP
     * CheckByGuest
     * ReportGuestIEP
     */
    [Route("api/[controller]")]
    public class LobbyController : Controller
    {
        private ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> _lobbies;
        public LobbyController(ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> lobbies)
        {
            _lobbies = lobbies;
        }

        // api/lobby/create
        [HttpGet("create")]
        public ActionResult Create()
        {
            Random rnd = new Random();
            Guid guid = Guid.NewGuid();
            int publickey = rnd.Next(100000, 1000000);
            int password = rnd.Next(1000, 10000);
            Lobby lobby = new Lobby();
            _lobbies.CreateEntry(guid, publickey, password, lobby);
            LobbyInfo lobbyInfo = new LobbyInfo(guid, publickey, password);
            return Json(lobbyInfo);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // api/lobby/delete
        [HttpDelete("delete")]
        public ActionResult Delete()
        {
            byte[] arr = new byte[52]; // 52 - length of serialized Guid
            // read string from body
            HttpContext.Request.Body.Read(arr, 0, arr.Length);
            string serialized = Encoding.UTF8.GetString(arr);
            // deserialize to dynamic to get access to any property
            dynamic deserialized = JsonConvert.DeserializeObject(serialized);
            // get guid from dynamic
            Guid guid = (Guid) deserialized.PrivateKey;
            // remove by privatekey
            if (_lobbies.TryRemove(guid))
                return NoContent();
            else
                return NotFound();
        }
    }
}
