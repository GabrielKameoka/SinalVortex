namespace SinalVortex.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;

public class NotificacaoDispatcher : INotificacaoDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificacaoDispatcher> _logger;

    public NotificacaoDispatcher(IServiceProvider serviceProvider, ILogger<NotificacaoDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        // Resolve os serviços registrados no escopo atual
        var services = _serviceProvider.GetServices<INotificacaoService>();
        var service = services.FirstOrDefault(s => s.Canal == item.Canal);

        if (service != null)
        {
            await service.EnviarAsync(item, cancellationToken);
            _logger.LogDebug("Notificação processada com sucesso. Conteúdo: {Conteudo}", item.Conteudo);
        }
        else
        {
            _logger.LogWarning("Nenhum serviço encontrado para o canal {Canal}", item.Canal);
        }
    }
}