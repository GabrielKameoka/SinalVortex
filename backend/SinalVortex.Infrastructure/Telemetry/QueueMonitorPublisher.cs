using System.Text.Json;
using SinalVortex.Application.Monitoring;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace SinalVortex.Infrastructure.Telemetry;

public sealed class QueueMonitorPublisher(IConnectionMultiplexer redis, ILogger<QueueMonitorPublisher> logger)
{
    internal const string ChannelName = "sinalvortex:queue-monitor";

    public async Task PublishAsync(Guid tenantId, string level, string message)
    {
        if (tenantId == Guid.Empty) return;
        try
        {
            var database = redis.GetDatabase();
            var lengths = await Task.WhenAll(
                database.ListLengthAsync("notificacoes:fila:alta"),
                database.ListLengthAsync("notificacoes:fila:normal"),
                database.ListLengthAsync("notificacoes:fila:baixa"),
                database.ListLengthAsync("notificacoes:fila:dlq"));

            var update = new QueueMonitorEventDto(
                tenantId,
                new QueueSnapshotDto((int)lengths[0], (int)lengths[1], (int)lengths[2], (int)lengths[3]),
                level,
                message,
                DateTime.UtcNow);

            await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(ChannelName), JsonSerializer.Serialize(update));
        }
        catch (Exception exception)
        {
            // Telemetry must never cause a delivered notification to be retried.
            logger.LogWarning(exception, "Não foi possível publicar evento do monitor de filas.");
        }
    }
}
