using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Entities;
using SinalVortex.Infrastructure.Persistence;

namespace SinalVortex.Infrastructure.Repositories;

public sealed class ContatoRepository(AppDbContext context) : IContatoRepository
{
    public async Task AdicionarAsync(Contato contato, CancellationToken cancellationToken)
    {
        await context.Contatos.AddAsync(contato, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<Contato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Contatos.FirstOrDefaultAsync(contato => contato.Id == id, cancellationToken);

    public async Task<(IReadOnlyCollection<Contato> Items, int TotalCount)> ObterPaginadoAsync(string? busca, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Contatos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(c => c.Nome.Contains(busca) || (c.Email != null && c.Email.Contains(busca)) || (c.Telefone != null && c.Telefone.Contains(busca)));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(c => c.Nome).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AtualizarAsync(Contato contato, CancellationToken cancellationToken = default)
    {
        context.Contatos.Update(contato);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(Contato contato, CancellationToken cancellationToken = default)
    {
        context.Contatos.Remove(contato);
        await context.SaveChangesAsync(cancellationToken);
    }
}
