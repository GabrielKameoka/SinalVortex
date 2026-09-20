namespace SinalVortex.Worker;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Contexts;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Exceptions;
using SinalVortex.Infrastructure.Telemetry;

public class SignalProcessingWorker(
    IServiceProvider serviceProvider, 
    ILogger<SignalProcessingWorker> logger,
    QueueMonitorPublisher queueMonitor) : BackgroundService
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

                    await queueMonitor.PublishAsync(item.TenantId, "info", $"Worker consumiu a fila {NomeFila(filaKey)} para processar a notificação {item.NotificacaoId}.");

                    if (item.TenantId == Guid.Empty)
                    {
                        logger.LogError("Mensagem {Id} sem TenantId. Movendo para DLQ para intervenção manual.", item.NotificacaoId);
                        await cacheService.EnqueueAsync(FilaDlqKey, item);
                        break;
                    }

                    // A fila carrega o tenant de origem; cada scope do Worker fica
                    // limitado ao mesmo filtro global usado nas requisições HTTP.
                    scope.ServiceProvider.GetRequiredService<TenantContext>().SetTenant(item.TenantId);
                    
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
        logger.LogInformation("[Processando ID: {Id}] Canal: {Canal} | Destinatario: {Destinatario}", item.NotificacaoId, item.Canal, item.Destinatario);

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
            await queueMonitor.PublishAsync(item.TenantId, "success", $"Notificação {item.NotificacaoId} processada com sucesso em {stopwatch.ElapsedMilliseconds} ms.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PermanentChannelException ex)
        {
            stopwatch.Stop();

            SinalVortexMetrics.NotificacoesFalhas.Add(1, new KeyValuePair<string, object?>("canal", item.Canal.ToString()));
            SinalVortexMetrics.TempoProcessamentoMs.Record(stopwatch.ElapsedMilliseconds);

            logger.LogError(ex, "[Falha Permanente] Erro definitivo na Notificação ID {Id}. Movendo direto para DLQ.", item.NotificacaoId);

            if (notificacao != null)
            {
                // Utiliza o método nativo do seu modelo de domínio
                notificacao.EnviarParaDlq(ex.Message);
        
                await repository.AtualizarAsync(notificacao, cancellationToken);
                await cacheService.EnqueueAsync(FilaDlqKey, item);
            }

            await queueMonitor.PublishAsync(item.TenantId, "error", $"Notificação {item.NotificacaoId} movida para a DLQ: {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            SinalVortexMetrics.NotificacoesFalhas.Add(1, new KeyValuePair<string, object?>("canal", item.Canal.ToString()));
            SinalVortexMetrics.TempoProcessamentoMs.Record(stopwatch.ElapsedMilliseconds);

            logger.LogWarning(ex, "[Falha Transiente] Erro temporário na Notificação ID {Id}.", item.NotificacaoId);

            if (notificacao != null)
            {
                notificacao.RegistrarFalha(ex.Message);
                await repository.AtualizarAsync(notificacao, cancellationToken);

                if (notificacao.Status == Domain.Enums.StatusNotificacao.Dlq)
                {
                    logger.LogError("[DLQ Engine] Limite de tentativas atingido para ID {Id}. Movendo para DLQ...", item.NotificacaoId);
                    await cacheService.EnqueueAsync(FilaDlqKey, item);
                    await queueMonitor.PublishAsync(item.TenantId, "error", $"Notificação {item.NotificacaoId} atingiu o limite de tentativas e foi movida para a DLQ.");
                }
                else
                {
                    logger.LogWarning("[Retry Engine] Devolvendo Notificação ID {Id} para a fila {Fila} (Tentativa {Tentativa})...", 
                        item.NotificacaoId, filaOrigemKey, notificacao.Tentativas);

                    await cacheService.EnqueueAsync(filaOrigemKey, item);
                    await queueMonitor.PublishAsync(item.TenantId, "warning", $"Notificação {item.NotificacaoId} retornou à fila {NomeFila(filaOrigemKey)} para nova tentativa.");
                }
            }
        }
    }

    private static string NomeFila(string key) => key.Split(':').Last();
}
