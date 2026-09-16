using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Infrastructure.Persistence;

namespace SinalVortex.Infrastructure.Repositories;

/// <summary>
/// Repositório destinado somente ao Worker para manutenção operacional.
/// O bypass do filtro global é deliberadamente confinado a esta implementação.
/// </summary>
public sealed class SystemNotificacaoRepository(AppDbContext context) : ISystemNotificacaoRepository
{
    public Task<int> RemoverNotificacoesAntigasAsync(DateTime dataCorte, CancellationToken cancellationToken = default) =>
        context.Notificacoes
            .IgnoreQueryFilters()
            .Where(n => n.CreatedAt < dataCorte &&
                        (n.Status == StatusNotificacao.Enviado || n.Status == StatusNotificacao.Dlq))
            .ExecuteDeleteAsync(cancellationToken);
}
