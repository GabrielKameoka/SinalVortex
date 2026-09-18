using System.Text.Json;
using SinalVortex.Application.Monitoring;
using StackExchange.Redis;

namespace SinalVortex.Worker;

public sealed class QueueMonitorPublisher(IConnectionMultiplexer redis)
{
    internal const string ChannelName = "sinalvortex:queue-monitor";

    public async Task PublishAsync(Guid tenantId, string level, string message)
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
}
