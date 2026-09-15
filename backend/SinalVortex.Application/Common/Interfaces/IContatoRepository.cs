namespace SinalVortex.Application.Common.Interfaces;

using SinalVortex.Domain.Entities;

public interface IContatoRepository
{
    Task AdicionarAsync(Contato contato, CancellationToken cancellationToken);
    Task<Contato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
}