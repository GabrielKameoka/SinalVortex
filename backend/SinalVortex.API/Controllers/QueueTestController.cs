using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Common.Interfaces;

namespace SinalVortex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueTestController : ControllerBase
{
    private readonly ICacheService _cacheService;

    public QueueTestController(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    [HttpPost("enviar-sinal")]
    public async Task<IActionResult> EnviarSinalParaFila([FromBody] string mensagem)
    {
        // Enfileira na chave "fila_sinais" (a mesma que o Worker escuta)
        await _cacheService.EnqueueAsync("fila_sinais", $"Sinal de Teste: {mensagem} às {DateTime.Now:HH:mm:ss}");
        
        return Ok(new { mensagem = "Sinal enviado para o Redis com sucesso!" });
    }
}