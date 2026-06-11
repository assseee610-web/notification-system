using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Web.Services;

namespace NotificationSystem.Web.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var results = await _healthCheckService.CheckAllAsync();
        return Ok(results);
    }
}