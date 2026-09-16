using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Models;
using SinalVortex.Infrastructure.Persistence;

namespace SinalVortex.Infrastructure.Repositories;

public sealed class TemplateRepository(AppDbContext context) : ITemplateRepository
{
    public async Task AdicionarAsync(Template template, CancellationToken cancellationToken = default) { await context.Templates.AddAsync(template, cancellationToken); await context.SaveChangesAsync(cancellationToken); }
    public Task<Template?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => context.Templates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    public async Task<(IReadOnlyCollection<Template> Items, int TotalCount)> ObterPaginadoAsync(string? busca, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Templates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca)) query = query.Where(t => t.Chave.Contains(busca));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(t => t.Chave).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
    public async Task AtualizarAsync(Template template, CancellationToken cancellationToken = default) { context.Templates.Update(template); await context.SaveChangesAsync(cancellationToken); }
    public async Task RemoverAsync(Template template, CancellationToken cancellationToken = default) { context.Templates.Remove(template); await context.SaveChangesAsync(cancellationToken); }
    public Task<bool> AplicacaoPertenceAoTenantAsync(Guid aplicacaoId, CancellationToken cancellationToken = default) => context.Aplicacoes.AnyAsync(a => a.Id == aplicacaoId, cancellationToken);
}
