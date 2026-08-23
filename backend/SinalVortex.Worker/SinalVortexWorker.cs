using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;

namespace SinalVortex.Worker;

public class SignalProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignalProcessingWorker> _logger;

    // Chaves das filas que a API utiliza
    private readonly string[] _filas = { "notificacoes:fila:alta", "notificacoes:fila:normal", "notificacoes:fila:baixa" };

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
                bool encontrouItem = false;

                // Processa respeitando a ordem de prioridade (Alta -> Normal -> Baixa)
                foreach (var filaKey in _filas)
                {
                    var item = await cacheService.DequeueAsync<NotificacaoFilaItemDto>(filaKey);

                    if (item != null)
                    {
                        _logger.LogInformation("[Processando Notificação ID: {Id}] Canal: {Canal} | Destinatário: {Destinatario}", 
                            item.NotificacaoId, item.Canal, item.Destinatario);
                        
                        // TODO: Chamar o serviço de envio do canal (E-mail/WhatsApp/SMS)
                        encontrouItem = true;
                        break; // Volta ao início para checar novamente a fila de prioridade Alta
                    }
                }

                if (!encontrouItem)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}