namespace SinalVortex.Worker;

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Infrastructure.Telemetry;

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
                using var scope = serviceProvider.CreateScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var item = await cacheService.DequeueAsync<NotificacaoFilaItemDto>(filaKey);

                if (item != null)
                {
                    encontrouItem = true;
                    
                    var dispatcher = scope.ServiceProvider.GetRequiredService<INotificacaoDispatcher>();
                    var repository = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();

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
        var stopwatch = Stopwatch.StartNew();

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

            stopwatch.Stop();
            
            SinalVortexMetrics.NotificacoesProcessadas.Add(1, new KeyValuePair<string, object?>("canal", item.Canal.ToString()));
            SinalVortexMetrics.TempoProcessamentoMs.Record(stopwatch.ElapsedMilliseconds);

            logger.LogInformation("[Sucesso] Notificação ID {Id} enviada em {Ms}ms.", item.NotificacaoId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            SinalVortexMetrics.NotificacoesFalhas.Add(1, new KeyValuePair<string, object?>("canal", item.Canal.ToString()));
            SinalVortexMetrics.TempoProcessamentoMs.Record(stopwatch.ElapsedMilliseconds);

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
                    logger.LogWarning("[Retry Engine] Devolvendo Notificação ID {Id} para a fila {Fila} (Tentativa {Tentativa})...", 
                        item.NotificacaoId, filaOrigemKey, notificacao.Tentativas);

                    await cacheService.EnqueueAsync(filaOrigemKey, item);
                }
            }
        }
    }
}