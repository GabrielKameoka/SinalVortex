using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SinalVortex.Application.Monitoring;
using StackExchange.Redis;

namespace SinalVortex.API.Hubs;

[Authorize]
public sealed class QueueMonitorHub(IConnectionMultiplexer redis) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantId, out _))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
        await base.OnConnectedAsync();
    }

    public async Task<QueueSnapshotDto> GetSnapshot()
    {
        var database = redis.GetDatabase();
        var lengths = await Task.WhenAll(
            database.ListLengthAsync("notificacoes:fila:alta"),
            database.ListLengthAsync("notificacoes:fila:normal"),
            database.ListLengthAsync("notificacoes:fila:baixa"),
            database.ListLengthAsync("notificacoes:fila:dlq"));

        return new QueueSnapshotDto((int)lengths[0], (int)lengths[1], (int)lengths[2], (int)lengths[3]);
    }

    public static string TenantGroup(string tenantId) => $"queue-monitor:{tenantId}";
}
