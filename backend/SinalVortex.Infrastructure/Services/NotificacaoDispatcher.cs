namespace SinalVortex.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;

public class NotificacaoDispatcher : INotificacaoDispatcher
{
    private readonly IDictionary<CanalNotificacao, INotificacaoService> _strategies;
    private readonly ILogger<NotificacaoDispatcher> _logger;

    public NotificacaoDispatcher(IEnumerable<INotificacaoService> services, ILogger<NotificacaoDispatcher> logger)
    {
        _logger = logger;
        // Mapeia em memória cada canal para sua respectiva estratégia
        _strategies = services.ToDictionary(s => s.Canal);
    }

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processando envio via Dispatcher para o canal [{Canal}]...", item.Canal);

        if (!_strategies.TryGetValue(item.Canal, out var strategy))
        {
            _logger.LogWarning("Nenhum provedor/estratégia configurado para o canal {Canal}.", item.Canal);
            throw new NotSupportedException($"Canal de notificação {item.Canal} não possui suporte ativo.");
        }

        // Executa a estratégia concreta com Polly e resiliência embutidos
        await strategy.EnviarAsync(item, cancellationToken);
    }
}