    using Microsoft.Extensions.Logging;
    using SinalVortex.Application.Common.Interfaces;
    using SinalVortex.Application.Commands.Notificacoes;
    using SinalVortex.Domain.Enums;

    namespace SinalVortex.Infrastructure.Services;

    public class NotificacaoDispatcher : INotificacaoDispatcher
    {
        private readonly Dictionary<CanalNotificacao, INotificacaoService> _services;
        private readonly ILogger<NotificacaoDispatcher> _logger;

        public NotificacaoDispatcher(IEnumerable<INotificacaoService> services, ILogger<NotificacaoDispatcher> logger)
        {
            _logger = logger;

            // Agrupa por canal e pega só o primeiro serviço de cada tipo
            _services = services
                .GroupBy(s => s.Canal)
                .ToDictionary(g => g.Key, g => g.First());

            _logger.LogInformation("NotificacaoDispatcher inicializado com {Count} serviços", _services.Count);
        }

        // Implementação da interface
        public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
        {
            if (_services.TryGetValue(item.Canal, out var service))
            {
                // Passa o DTO inteiro para o serviço
                await service.EnviarAsync(item, cancellationToken);

                _logger.LogDebug("Conteúdo da notificação: {Conteudo}", item.Conteudo);
            }
            else
            {
                _logger.LogWarning("Nenhum serviço encontrado para o canal {Canal}", item.Canal);
            }
        }



        
    }