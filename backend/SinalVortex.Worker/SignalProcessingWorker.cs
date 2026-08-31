namespace SinalVortex.Worker;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;

public class SignalProcessingWorker(
    IServiceProvider serviceProvider, 
    ILogger<SignalProcessingWorker> logger) : BackgroundService
{
    private readonly string[] _filas = ["notificacoes:fila:alta", "notificacoes:fila:normal", "notificacoes:fila:baixa"];
    private const string FilaDlqKey = "notificacoes:fila:dlq";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SignalProcessingWorker escutando filas do Redis via ICacheService...");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<INotificacaoDispatcher>();
            var repository = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();

            bool encontrouItem = false;

            foreach (var filaKey in _filas)
            {
                var item = await cacheService.DequeueAsync<NotificacaoFilaItemDto>(filaKey);

                if (item != null)
                {
                    encontrouItem = true;
                    await ProcessarItemAsync(item, filaKey, cacheService, dispatcher, repository, stoppingToken);
                    break;
                }
            }

            if (!encontrouItem)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessarItemAsync(
        NotificacaoFilaItemDto item,
        string filaOrigemKey,
        ICacheService cacheService,
        INotificacaoDispatcher dispatcher,
        INotificacaoRepository repository,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("[Processando ID: {Id}] Canal: {Canal}", item.NotificacaoId, item.Canal);

        var notificacao = await repository.ObterPorIdAsync(item.NotificacaoId, cancellationToken);

        try
        {
            if (notificacao != null)
            {
                notificacao.IniciarProcessamento();
                await repository.AtualizarAsync(notificacao, cancellationToken);
            }

            await dispatcher.EnviarAsync(item, cancellationToken);

            if (notificacao != null)
            {
                notificacao.MarcarComoEnviado();
                await repository.AtualizarAsync(notificacao, cancellationToken);
            }

            logger.LogInformation("[Sucesso] Notificação ID {Id} enviada.", item.NotificacaoId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Falha] Erro no processamento da Notificação ID {Id}.", item.NotificacaoId);

            if (notificacao != null)
            {
                notificacao.RegistrarFalha(ex.Message);
                await repository.AtualizarAsync(notificacao, cancellationToken);

                if (notificacao.Status == Domain.Enums.StatusNotificacao.Dlq)
                {
                    logger.LogError("[DLQ Engine] Limite de tentativas atingido para ID {Id}. Movendo para DLQ...", item.NotificacaoId);
                    await cacheService.EnqueueAsync(FilaDlqKey, item);
                }
                else
                {
                    var segundosEspera = Math.Pow(2, notificacao.Tentativas);
                    logger.LogWarning("[Retry Engine] Aguardando {Segundos}s para re-enfileirar a Notificação ID {Id}...", 
                        segundosEspera, item.NotificacaoId);

                    await Task.Delay(TimeSpan.FromSeconds(segundosEspera), cancellationToken);
                    await cacheService.EnqueueAsync(filaOrigemKey, item);
                }
            }
        }
    }
}