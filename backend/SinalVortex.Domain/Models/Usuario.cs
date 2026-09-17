using SinalVortex.Domain.Common;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.Entities;

public sealed class Usuario : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool Ativo { get; private set; }

    private Usuario() { }

    public Usuario(Guid tenantId, string nome, string email, string passwordHash) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("A credencial do usuário é obrigatória.");

        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Ativo = true;
    }
}
