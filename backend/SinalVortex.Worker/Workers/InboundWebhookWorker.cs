namespace SinalVortex.Worker.Workers;

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Webhooks;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Dtos.Webhooks;

public class InboundWebhookWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboundWebhookWorker> _logger;

    public InboundWebhookWorker(IServiceProvider serviceProvider, ILogger<InboundWebhookWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de Inbound Webhooks iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

                var payload = await cacheService.DequeueAsync<InboundWebhookPayloadDto>("webhooks:inbound:fila");

                if (payload != null)
                {
                    _logger.LogInformation("[Webhook Worker] Processando evento {Evento} para notificação {NotificacaoId}", payload.Evento, payload.NotificacaoId);
                    await mediator.Send(new ProcessarInboundWebhookCommand(payload), stoppingToken);
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento do evento de webhook.");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}