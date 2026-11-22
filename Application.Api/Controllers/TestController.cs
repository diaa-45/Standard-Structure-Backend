using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public TestController() { }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Test endpoint is working!" });
        }
    }
}
