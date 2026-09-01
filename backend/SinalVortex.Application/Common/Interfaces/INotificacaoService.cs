using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Domain.Enums;

namespace SinalVortex.Application.Common.Interfaces;

public interface INotificacaoService
{
    CanalNotificacao Canal { get; }
    Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken);
}