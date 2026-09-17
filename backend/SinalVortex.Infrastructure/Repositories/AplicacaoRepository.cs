using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Models;
using SinalVortex.Infrastructure.Persistence;

namespace SinalVortex.Infrastructure.Repositories;

public sealed class AplicacaoRepository(AppDbContext context) : IAplicacaoRepository
{
    public async Task<IReadOnlyCollection<Aplicacoes>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.Aplicacoes.AsNoTracking().OrderBy(a => a.Nome).ToListAsync(cancellationToken);

    public Task<Aplicacoes?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Aplicacoes.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AdicionarAsync(Aplicacoes aplicacao, CancellationToken cancellationToken = default)
    {
        await context.Aplicacoes.AddAsync(aplicacao, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
