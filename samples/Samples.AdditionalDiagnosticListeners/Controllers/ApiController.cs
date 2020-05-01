// Modified by SignalFx
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Samples.AdditionalDiagnosticListeners.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        [HttpGet]
        public IActionResult SomeAction()
        {
            return Ok();
        }
    }
}
