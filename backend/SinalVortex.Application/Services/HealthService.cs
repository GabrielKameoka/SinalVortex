namespace SinalVortex.Application.Services;

public interface IHealthService
{
    Task<HealthCheckResponse> GetHealthAsync();
}

public class HealthCheckResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HealthService : IHealthService
{
    public Task<HealthCheckResponse> GetHealthAsync()
    {
        return Task.FromResult(new HealthCheckResponse());
    }
}
