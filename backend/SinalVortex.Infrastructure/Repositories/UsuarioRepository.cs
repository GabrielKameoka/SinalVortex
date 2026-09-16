using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Entities;
using SinalVortex.Infrastructure.Persistence;

namespace SinalVortex.Infrastructure.Repositories;

public sealed class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await context.Usuarios.AddAsync(usuario, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<Usuario?> ObterParaAutenticacaoAsync(string email, Guid tenantId, CancellationToken cancellationToken = default) =>
        // Antes da autenticação não existe TenantContext. A remoção do filtro fica
        // confinada neste repositório e a consulta sempre inclui o TenantId fornecido.
        context.Usuarios.IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email.Trim().ToLowerInvariant(), cancellationToken);
}
