using SinalVortex.Application.Common.Interfaces;

namespace SinalVortex.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Services;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;
    private readonly ICacheService _cacheService;

    public HealthController(IHealthService healthService, ICacheService cacheService)
    {
        _healthService = healthService;
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var response = await _healthService.GetHealthAsync();
        return Ok(response);
    }
    
    [HttpGet("redis-test")]
    public async Task<IActionResult> TestRedis()
    {
        var testKey = "api_railway_ping";
        var testValue = $"API operacional em {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        // 1. Grava no Redis via API
        await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(2));

        // 2. Lê de volta do Redis
        var result = await _cacheService.GetAsync<string>(testKey);

        return Ok(new
        {
            Status = "Sucesso",
            Mensagem = "Comunicação com Redis na Railway confirmada!",
            ValorRecuperado = result
        });
    }
}
