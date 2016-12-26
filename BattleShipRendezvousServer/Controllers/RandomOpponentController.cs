using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private IMemoryCache cache;
        public RandomOpponentController(IMemoryCache ca_che)
        {
            cache = ca_che;
        }

        // Get api/randomopponent/
        [HttpGet]
        public async JsonResult Get()
        {
            return Json(DateTime.Now);
        }

        public class Info
        {
            public string info { get; set; }
        }
        [HttpPost("{id}")]
        public string Post(int id, [FromBody] Info info)
        {
            byte[] arr = new byte[10000];
            var x = HttpContext.Request.Body.Read(arr, 0, arr.Length);
            string str = Encoding.UTF8.GetString(arr, 0, x);
            Response.Cookies.Append("My_Cookie", "add in controller", new CookieOptions()
            {
                Expires = DateTime.Now.AddMinutes(1),
            Path = "/",
            Secure = false,
            HttpOnly = false,
            });
            return ToString();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
