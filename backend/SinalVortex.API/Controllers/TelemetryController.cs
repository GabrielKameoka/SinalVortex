namespace SinalVortex.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Common.Interfaces;
using StackExchange.Redis;

[ApiController]
[Route("api/v1/metrics")]
public class TelemetryController(IConnectionMultiplexer redis) : ControllerBase
{
    private static readonly string[] Filas = ["notificacoes:fila:alta", "notificacoes:fila:normal", "notificacoes:fila:baixa", "notificacoes:fila:dlq"];

    [HttpGet]
    public async Task<IActionResult> ObterMetricasFilas()
    {
        var db = redis.GetDatabase();
        var contagemFilas = new Dictionary<string, long>();

        foreach (var fila in Filas)
        {
            var tamanho = await db.ListLengthAsync(fila);
            contagemFilas[fila] = tamanho;
        }

        return Ok(new
        {
            Status = "OK",
            Timestamp = DateTime.UtcNow,
            Filas = contagemFilas
        });
    }
}