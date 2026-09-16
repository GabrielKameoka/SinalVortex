namespace SinalVortex.Application.Common.Interfaces;

using SinalVortex.Domain.Entities;

public interface IContatoRepository
{
    Task AdicionarAsync(Contato contato, CancellationToken cancellationToken = default);
    Task<Contato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<Contato> Items, int TotalCount)> ObterPaginadoAsync(string? busca, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Contato contato, CancellationToken cancellationToken = default);
    Task RemoverAsync(Contato contato, CancellationToken cancellationToken = default);
}
