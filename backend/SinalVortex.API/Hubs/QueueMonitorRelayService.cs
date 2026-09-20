using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SinalVortex.Application.Monitoring;
using StackExchange.Redis;

namespace SinalVortex.API.Hubs;

public sealed class QueueMonitorRelayService(
    IConnectionMultiplexer redis,
    IHubContext<QueueMonitorHub> hub,
    ILogger<QueueMonitorRelayService> logger) : IHostedService
{
    private ChannelMessageQueue? _subscription;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = await redis.GetSubscriber().SubscribeAsync(RedisChannel.Literal("sinalvortex:queue-monitor"));
        _subscription.OnMessage(async message =>
        {
            try
            {
                var update = JsonSerializer.Deserialize<QueueMonitorEventDto>(message.Message.ToString());
                if (update is not null)
                    await hub.Clients.Group(QueueMonitorHub.TenantGroup(update.TenantId.ToString())).SendAsync("queueUpdate", update);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Não foi possível retransmitir atualização do monitor de filas.");
            }
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscription is not null)
            await _subscription.UnsubscribeAsync();
    }
}
