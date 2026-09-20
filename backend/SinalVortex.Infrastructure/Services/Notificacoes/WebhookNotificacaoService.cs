using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Infrastructure.Services.Notificacoes;

public sealed class WebhookNotificacaoService(IHttpClientFactory clients, IConfiguration configuration) : INotificacaoService
{
    public CanalNotificacao Canal => CanalNotificacao.Webhook;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        var allowed = configuration.GetSection("WebhookSettings:AllowedOrigins").Get<string[]>() ?? [];
        if (!Uri.TryCreate(item.Destinatario, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) ||
            !allowed.Contains(uri.GetLeftPart(UriPartial.Authority), StringComparer.OrdinalIgnoreCase))
            throw new PermanentChannelException("Destino Webhook não autorizado. Configure sua origem HTTPS em WebhookSettings:AllowedOrigins.");

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(new { item.NotificacaoId, item.Assunto, item.Conteudo })
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", item.NotificacaoId.ToString());
        using var response = await clients.CreateClient("notification-webhook").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.IsSuccessStatusCode) return;
        var status = (int)response.StatusCode;
        if (status >= 500 || status is 408 or 429)
            throw new TransientChannelException($"Webhook temporariamente indisponível (HTTP {status}).");
        throw new PermanentChannelException($"Webhook recusado (HTTP {status}).");
    }
}
