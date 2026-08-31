using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Common.Interfaces;

namespace SinalVortex.Worker.Workers;

public class LimpezaNotificacoesWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LimpezaNotificacoesWorker> _logger;
    private static readonly TimeSpan IntervaloExecucao = TimeSpan.FromHours(24);

    public LimpezaNotificacoesWorker(IServiceProvider serviceProvider, ILogger<LimpezaNotificacoesWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de Purge/Limpeza de Notificações Antigas iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();

                var dataCorte = DateTime.UtcNow.AddDays(-30);
                _logger.LogInformation("Executando expiração de registros anteriores a {DataCorte}...", dataCorte);

                int totalRemovidas = await repository.RemoverNotificacoesAntigasAsync(dataCorte, stoppingToken);

                _logger.LogInformation("Expiração concluída. Total de {Total} notificações removidas.", totalRemovidas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar o purge de notificações antigas.");
            }

            await Task.Delay(IntervaloExecucao, stoppingToken);
        }
    }
}