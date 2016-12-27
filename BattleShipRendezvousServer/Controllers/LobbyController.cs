using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BattleShipRendezvousServer.Dependency_Injection;
using BattleShipRendezvousServer.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace BattleShipRendezvousServer.Controllers
{
    [Route("api/[controller]")]
    public class LobbyController : Controller
    {
        private ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> _lobbies;
        public LobbyController(ICacheWithPublicPrivateKeys<Guid, int, int, Lobby> lobbies)
        {
            _lobbies = lobbies;
        }

        // api/lobby/create
        [HttpGet("Create")]
        public ActionResult Create()
        {
            // generate random lobby info
            Random rnd = new Random();
            Guid guid = Guid.NewGuid();
            int publickey = rnd.Next(100000, 1000000);
            int password = rnd.Next(1000, 10000);
            Lobby lobby = new Lobby();

            // insert lobby to cache
            _lobbies.CreateEntry(guid, publickey, password, lobby);

            // report lobbyInfo
            LobbyInfo lobbyInfo = new LobbyInfo(guid, publickey, password);
            return Json(lobbyInfo);
        }

        // api/lobby/reportguestready/?publickey=0&password=0
        [HttpPut("ReportGuestReady")]
        public ActionResult ReportGuestReady(int publickey, int password)
        {
            // try get lobby by publickey and password
            Lobby lobby;
            if (!_lobbies.TryGetValueByPublicKey(publickey, password, out lobby))
                return NotFound();

            // report guest ready
            lobby.GuestReady = true;
            return NoContent();
        }

        // api/lobby/checkmylobby/aa-aa-aa-aa
        [HttpGet("CheckMyLobby/{privatekey}")]
        public ActionResult CheckMyLobby(Guid privatekey)
        {
            ICacheWithPublicPrivateKeysEntry<Guid, int, int, Lobby> entry;
            // try find entry
            if (!_lobbies.TryGetEntryByPrivateKey(privatekey, out entry))
                return NotFound(); // if not found - error code
            // report that guest ready
            return Json(new {GuestReady = entry.Value.GuestReady});
        }


        // api/lobby/reportowneriep/aa-aa-aa-aa
        [HttpPut("ReportOwnerIEP/{privatekey}")]
        public ActionResult ReportOwnerIEP(Guid privatekey, [FromBody] dynamic ownerinfo)
        {
            ICacheWithPublicPrivateKeysEntry<Guid, int, int, Lobby> entry;
            // try find entry
            if (!_lobbies.TryGetEntryByPrivateKey(privatekey, out entry))
                return NotFound(); // if not found - error code

            // save iep from body to lobby
            entry.Value.OwnerIEP = ownerinfo.OwnerIEP;

            // report guest IEP
            return Json(new { GuestIEP = entry.Value.GuestIEP });
        }

        // api/lobby/reportguestiep/?publickey=0&password=0
        [HttpPut("ReportGuestIEP")]
        public ActionResult ReportGuestIEP(int publickey, int password, [FromBody] dynamic guestinfo)
        {
            // try get lobby by publickey and password
            Lobby lobby;
            if (!_lobbies.TryGetValueByPublicKey(publickey, password, out lobby))
                return NotFound();

            // save guest iep from body to lobby
            lobby.GuestIEP = guestinfo.GuestIEP;

            // report owner iep
            return Json(new {OwnerIEP = lobby.OwnerIEP});
        }


        // api/lobby/delete/aa-aa-aa-aa
        [HttpDelete("Delete/{privatekey}")]
        public ActionResult Delete(Guid privatekey)
        {
            // remove by privatekey
            if (_lobbies.TryRemove(privatekey))
                return NoContent();
            else
                return NotFound();
        }
    }
}
