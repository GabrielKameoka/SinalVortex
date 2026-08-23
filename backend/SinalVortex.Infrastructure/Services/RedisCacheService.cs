using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SinalVortex.Application.Common.Interfaces;
using StackExchange.Redis;

namespace SinalVortex.Infrastructure.Services;

/// <summary>
/// Implementação concreta do serviço de cache e mensageria em memória utilizando o Redis.
/// Atua como camada intermediária entre a aplicação, o armazenamento distribuído e as filas assíncronas.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;

    /// <param name="distributedCache">Provedor de cache distribuído nativo do .NET (IDistributedCache).</param>
    /// <param name="redis">Gerenciador de conexões multiplexadas com o servidor Redis (StackExchange.Redis).</param>
    public RedisCacheService(IDistributedCache distributedCache, IConnectionMultiplexer redis)
    {
        _distributedCache = distributedCache;
        _redis = redis;
    }

    /// <summary>
    /// Armazena um objeto no cache do Redis de forma assíncrona com um tempo de expiração definido.
    /// O objeto é serializado no formato JSON antes do armazenamento.
    /// </summary>
    /// /// <typeparam name="T">O tipo do objeto a ser armazenado no cache.</typeparam>
    /// <param name="key">A chave única que identifica o registro no Redis.</param>
    /// <param name="value">A instância do objeto a ser persistida no cache.</param>
    /// <param name="expiration">Tempo de vida limite do registro (TTL). Se não informado, o padrão é 60 minutos.</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions // Tempo se expiração que garante que o dado não fique armazenado eternamente na memória
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(60)
        };

        var json = JsonSerializer.Serialize(value);
        await _distributedCache.SetStringAsync(key, json, options);
    }

    /// <summary>
    /// Recupera um objeto do cache do Redis a partir da sua chave de identificação.
    /// </summary>
    /// <typeparam name="T">O tipo do objeto esperado após a desserialização.</typeparam>
    /// <param name="key">A chave única referente ao registro desejado.</param>
    /// <returns>
    /// Retorna o objeto desserializado do tipo <typeparamref name="T"/> caso a chave exista e esteja válida; 
    /// caso contrário, retorna <c>null</c>.
    /// </returns>
    public async Task<T?> GetAsync<T>(string key){
        var json = await _distributedCache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
            return default;

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task RemoveAsync(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }

    /// <summary>
    /// Adiciona uma mensagem no início (ponta esquerda) de uma estrutura de lista no Redis (Enqueue).
    /// Utilizado para enfileiramento de eventos e tarefas assíncronas enviadas à camada de processamento.
    /// </summary>
    /// <typeparam name="T">O tipo da mensagem ou evento enfileirado.</typeparam>
    /// <param name="queueName">O identificador/nome da fila no servidor Redis.</param>
    /// <param name="item">O objeto a ser serializado e inserido na fila.</param>
    public async Task EnqueueAsync<T>(string queueName, T item)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(item);
        // Adicionamos o prefixo manualmente se quiser manter a consistência com o IDistributedCache
        await db.ListLeftPushAsync($"SinalVortex_{queueName}", json);
    }

    /// <summary>
    /// Remove e retorna o item mais antigo (ponta direita) presente na lista do Redis (Dequeue).
    /// Garante o padrão FIFO (First In, First Out) no consumo de mensagens pelos Workers em segundo plano.
    /// </summary>
    /// <typeparam name="T">O tipo do objeto retornado e desserializado.</typeparam>
    /// <param name="queueName">O identificador/nome da fila a ser consumida.</param>
    /// <returns>
    /// A mensagem desserializada do tipo <typeparamref name="T"/>, ou <c>null</c> se a fila estiver vazia.
    /// </returns>
    public async Task<T?> DequeueAsync<T>(string queueName)
    {
        var db = _redis.GetDatabase();
        RedisValue redisValue = await db.ListRightPopAsync($"SinalVortex_{queueName}");

        if (redisValue.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(redisValue.ToString()!);
    }
}