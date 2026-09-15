namespace SinalVortex.API.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Dtos.Webhooks;
using SinalVortex.Infrastructure.Services.Webhooks;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IWebhookSignatureValidator _signatureValidator;
    private readonly IConfiguration _configuration;

    public WebhooksController(
        ICacheService cacheService,
        IWebhookSignatureValidator signatureValidator,
        IConfiguration configuration)
    {
        _cacheService = cacheService;
        _signatureValidator = signatureValidator;
        _configuration = configuration;
    }

    [HttpPost("{provedor}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceberWebhook(
        [FromRoute] string provedor,
        [FromBody] InboundWebhookPayloadDto payload,
        [FromHeader(Name = "X-SinalVortex-Signature")] string? assinatura)
    {
        var secret = _configuration[$"Webhooks:{provedor}:Secret"] ?? "sinalvortex_default_secret";

        if (_configuration.GetValue<bool>("Webhooks:ValidarAssinatura"))
        {
            var bodyJson = System.Text.Json.JsonSerializer.Serialize(payload);
            if (!_signatureValidator.ValidarAssinatura(bodyJson, assinatura ?? "", secret))
            {
                return Unauthorized(new { Mensagem = "Assinatura do webhook inválida." });
            }
        }

        // Enfileira na chave de Redis de inbound
        await _cacheService.EnqueueAsync("webhooks:inbound:fila", payload);

        return Accepted(new { Mensagem = "Webhook recebido e enfileirado para processamento." });
    }
}