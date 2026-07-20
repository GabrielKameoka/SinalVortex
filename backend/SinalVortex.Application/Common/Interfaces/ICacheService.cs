namespace SinalVortex.Application.Common.Interfaces;

public interface ICacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task<T?> GetAsync<T>(string key);
    Task RemoveAsync(string key);
    
    // Métodos para suporte a filas/buffer (Redis List)
    Task EnqueueAsync<T>(string queueName, T item);
    Task<T?> DequeueAsync<T>(string queueName);
}