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
            bool encontrouItem = false;

            foreach (var filaKey in _filas)
            {
                // Resolve as dependências apenas quando precisa processar um item
                using var scope = serviceProvider.CreateScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var item = await cacheService.DequeueAsync<NotificacaoFilaItemDto>(filaKey);

                if (item != null)
                {
                    encontrouItem = true;
                    
                    var dispatcher = scope.ServiceProvider.GetRequiredService<INotificacaoDispatcher>();
                    var repository = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();

                    await ProcessarItemAsync(item, filaKey, cacheService, dispatcher, repository, stoppingToken);
                    break; // Mantém a prioridade da fila alta voltando ao topo do loop
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
                    // Re-enfileiramento imediato para não bloquear a thread do Worker.
                    // O controle de estado/tentativas já foi atualizado na entidade via repositório.
                    logger.LogWarning("[Retry Engine] Devolvendo Notificação ID {Id} para a fila {Fila} (Tentativa {Tentativa})...", 
                        item.NotificacaoId, filaOrigemKey, notificacao.Tentativas);

                    await cacheService.EnqueueAsync(filaOrigemKey, item);
                }
            }
        }
    }
}