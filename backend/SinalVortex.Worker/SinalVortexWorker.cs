using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Common.Interfaces;

namespace SinalVortex.Worker;

public class SignalProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignalProcessingWorker> _logger;

    public SignalProcessingWorker(IServiceProvider serviceProvider, ILogger<SignalProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SinalVortex Worker escutando filas do Redis...");

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                // Consome da fila de sinais via Dequeue (Pop da ponta direita)
                var sinal = await cacheService.DequeueAsync<string>("fila_sinais");

                if (sinal != null)
                {
                    _logger.LogInformation($"[Processando Sinal]: {sinal}");
                    // TODO: Aqui entra a lógica de processar o sinal/alerta
                }
                else
                {
                    // Se a fila estiver vazia, aguarda 1 segundo antes de tentar novamente (evita uso de CPU em 100%)
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}