using GameLib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AuthWebApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
       
        // GET api/games/GetGames
        [HttpGet]
        [Authorize(Roles = "read")]
        public ActionResult Get()
        {
            return Ok();
        }
   
        // POST api/games/AddGame
        [HttpPost]        
        [Authorize(Roles = "write")]
        public ActionResult Post([FromBody] Game game)
        {            
            return Ok();
        }

        // DELETE api/games/DeleteGame/1
        [HttpDelete("{id}")]        
        [Authorize(Roles = "delete")]
        public ActionResult Delete(int id)
        {
            return Ok();
        }
    }
}
