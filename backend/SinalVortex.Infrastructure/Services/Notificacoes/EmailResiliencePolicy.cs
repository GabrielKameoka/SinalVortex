namespace SinalVortex.Infrastructure.Services.Notificacoes;

using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Threading.Tasks;

public interface IEmailResiliencePolicy
{
    ResiliencePipeline Pipeline { get; }
}

public class EmailResiliencePolicy : IEmailResiliencePolicy
{
    public ResiliencePipeline Pipeline { get; }

    public EmailResiliencePolicy(ILogger<EmailResiliencePolicy> logger)
    {
        Pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnOpened = args =>
                {
                    logger.LogWarning(
                        "[Circuit Breaker] Provedor Principal ABERTO por {Seconds}s. Erro: {Message}", 
                        args.BreakDuration.TotalSeconds, 
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("[Circuit Breaker] Provedor FECHADO e operando normalmente.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("[Circuit Breaker] Provedor em teste (Half-Open)...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}