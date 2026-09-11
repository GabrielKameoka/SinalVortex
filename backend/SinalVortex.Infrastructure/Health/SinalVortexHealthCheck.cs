namespace SinalVortex.Infrastructure.Health;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SinalVortex.Infrastructure.Persistence;
using StackExchange.Redis;

public class SinalVortexHealthCheck(AppDbContext dbContext, IConnectionMultiplexer redis) : IHealthCheck
{
    // Valida conexões ativas do banco e do cache
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        // 1. Validar PostgreSQL
        try
        {
            var canConnectDb = await dbContext.Database.CanConnectAsync(cancellationToken);
            data["PostgreSQL"] = canConnectDb ? "Healthy" : "Unhealthy";
        }
        catch (Exception ex)
        {
            data["PostgreSQL"] = $"Unhealthy: {ex.Message}";
        }

        // 2. Validar Redis
        try
        {
            var isRedisConnected = redis.IsConnected;
            data["Redis"] = isRedisConnected ? "Healthy" : "Unhealthy";
        }
        catch (Exception ex)
        {
            data["Redis"] = $"Unhealthy: {ex.Message}";
        }

        bool isHealthy = data.Values.All(v => v.ToString() == "Healthy");

        return isHealthy 
            ? HealthCheckResult.Healthy("Todos os serviços de infraestrutura estão operacionais.", data)
            : HealthCheckResult.Unhealthy("Falha em um ou mais serviços de infraestrutura.", data: data);
    }
}