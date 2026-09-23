using SinalVortex.Domain.Entities;

namespace SinalVortex.Application.Common.Interfaces;

public interface IUsuarioRepository
{
    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task<bool> EmailJaRegistradoAsync(string email, CancellationToken cancellationToken = default);

    // Fluxo de autenticação: o tenant ainda não foi estabelecido e a busca é
    // limitada explicitamente por e-mail e TenantId.
    Task<Usuario?> ObterParaAutenticacaoAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);
}
