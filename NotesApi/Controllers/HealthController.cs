using Microsoft.AspNetCore.Mvc;

namespace NotesApi.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    // GET /api/health -> usado por la PWA para saber si el backend está disponible
    [HttpGet]
    public IActionResult Get() => Ok(new { Status = "ok" });
}