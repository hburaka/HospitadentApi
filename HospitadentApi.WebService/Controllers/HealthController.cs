using Microsoft.AspNetCore.Mvc;
using System;

namespace HospitadentApi.WebService.Controllers
{
    [Route("/")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "OK", message = "Hospitadent API çalışıyor", timestamp = DateTime.UtcNow });
        }
    }
}

