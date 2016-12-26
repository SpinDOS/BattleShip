using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShipRendezvousServer.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BattleShipRendezvousServer.Controllers
{
    [Route("api/[controller]")]
    public class RandomOpponentController : Controller
    {
        private LobbyCollection _lobbies;
        public RandomOpponentController(LobbyCollection lobbies)
        {
            _lobbies = lobbies;
        }

        // Get api/randomopponent/
        [HttpGet]
        public JsonResult Get()
        {
            LobbyInfo lobbyInfo;
            if (_lobbies.TryGetRandomOpponent(out lobbyInfo))
            {
                lobbyInfo.Lobby.GuestReady = true;
                return Json(new {FoundOpponent = true, PublicID = lobbyInfo.PublicId, Password = lobbyInfo.Password});
            }
            else
            {
                return Json(new { FoundOpponent = false, PrivateID = lobbyInfo.PrivateId});
            }
        }
    }
}
