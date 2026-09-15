using SinalVortex.Domain.Common;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.Models;

public class Aplicacoes : BaseEntity
{
    public string Nome { get; private set; }
    public string ApiKeyHash { get; private set; }
    public bool Ativo { get; private set; }

    // Construtor EF Core
    private Aplicacoes() { }

    public Aplicacoes(Guid tenantId, string nome, string apiKeyHash) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da aplicação não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(apiKeyHash))
            throw new DomainException("O hash da API Key é obrigatório.");

        Nome = nome.Trim();
        ApiKeyHash = apiKeyHash;
        Ativo = true;
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("A aplicação já está inativa.");

        Ativo = false;
        Touch();
    }

    public void Reativar()
    {
        if (Ativo)
            throw new DomainException("A aplicação já está ativa.");

        Ativo = true;
        Touch();
    }

    public void AtualizarApiKey(string novoApiKeyHash)
    {
        if (string.IsNullOrWhiteSpace(novoApiKeyHash))
            throw new DomainException("O novo hash da API Key é obrigatório.");

        ApiKeyHash = novoApiKeyHash;
        Touch();
    }
}