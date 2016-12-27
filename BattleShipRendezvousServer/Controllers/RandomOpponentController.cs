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
            //if (_searcher.TryGetOpponent(out lobbyInfo))
            //{
            //    return Json(lobbyInfo);
            //}
            //else
            {
                return Json(lobbyInfo);
            }
        }
    }
}
