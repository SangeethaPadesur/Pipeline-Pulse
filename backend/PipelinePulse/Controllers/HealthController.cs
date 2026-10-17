using Microsoft.AspNetCore.Mvc;
using PipelinePulse.Services;

namespace PipelinePulse.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }
    [HttpGet]
    public IActionResult GetHealth()
    {
        var healthStatus = _healthService.GetHealth();

        return healthStatus.Status == "ok"
            ? Ok(healthStatus)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, healthStatus);
    }
}