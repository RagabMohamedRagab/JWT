using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JWT.Controllers {
   [ Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    
    public class SecuredController : ControllerBase {

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hello From Secured Controller");
        }
    }
}
