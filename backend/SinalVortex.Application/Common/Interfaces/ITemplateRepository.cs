using SinalVortex.Domain.Models;

namespace SinalVortex.Application.Common.Interfaces;

public interface ITemplateRepository
{
    Task AdicionarAsync(Template template, CancellationToken cancellationToken = default);
    Task<Template?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<Template> Items, int TotalCount)> ObterPaginadoAsync(string? busca, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Template template, CancellationToken cancellationToken = default);
    Task RemoverAsync(Template template, CancellationToken cancellationToken = default);
    Task<bool> AplicacaoPertenceAoTenantAsync(Guid aplicacaoId, CancellationToken cancellationToken = default);
}
