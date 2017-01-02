using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShipRendezvousServer.Dependency_Injection;
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
        private RandomOpponentSearch _searcher;
        public RandomOpponentController(RandomOpponentSearch searcher)
        {
            _searcher = searcher;
        }

        // Get api/randomopponent/
        [HttpGet]
        public JsonResult Get()
        {
            LobbyInfo lobbyInfo = null;
            // get enemy
            if (_searcher.TryGetEnemy(out lobbyInfo)) // if found enemy
            {
                return Json(new {found = true,
                    publicKey = lobbyInfo.PublicKey, password = lobbyInfo.Password});
            }
            else // if you have to wait for another enemy
            {
                return Json(new {found = false, privateKey = lobbyInfo.PrivateKey});
            }
        }
    }
}
