using SinalVortex.Domain.Models;

namespace SinalVortex.Application.Common.Interfaces;

public interface IAplicacaoRepository
{
    Task<IReadOnlyCollection<Aplicacoes>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Aplicacoes?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Aplicacoes aplicacao, CancellationToken cancellationToken = default);
}
