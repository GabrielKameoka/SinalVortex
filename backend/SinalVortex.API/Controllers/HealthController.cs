namespace SinalVortex.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Services;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var response = await _healthService.GetHealthAsync();
        return Ok(response);
    }
}
