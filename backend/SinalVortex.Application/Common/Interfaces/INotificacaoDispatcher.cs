namespace SinalVortex.Application.Common.Interfaces;

using SinalVortex.Application.Commands.Notificacoes;

public interface INotificacaoDispatcher
{
    Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken);
}