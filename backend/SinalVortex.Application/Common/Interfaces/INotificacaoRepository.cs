namespace SinalVortex.Application.Common.Interfaces;

using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Models;

public interface INotificacaoRepository
{
    Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken = default);
    
    Task<Notificacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Notificacao notificacao, CancellationToken cancellationToken = default);
    
    Task<int> RemoverNotificacoesAntigasAsync(DateTime dataCorte, CancellationToken cancellationToken = default);
    
    Task<(IReadOnlyCollection<Notificacao> Items, int TotalCount)> ObterPaginadoAsync(
        Guid? aplicacaoId,
        StatusNotificacao? status,
        CanalNotificacao? canal,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}