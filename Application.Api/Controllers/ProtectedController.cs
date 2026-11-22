using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProtectedController : ControllerBase
    {
        [Authorize]
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { Message = "pong", User = User.Identity?.Name });
    }
}
