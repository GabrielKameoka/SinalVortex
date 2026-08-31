using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.Models;

public class Aplicacoes
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string ApiKeyHash { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor EF Core
    private Aplicacoes() { }

    public Aplicacoes(string nome, string apiKeyHash)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da aplicação não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(apiKeyHash))
            throw new DomainException("O hash da API Key é obrigatório.");

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        ApiKeyHash = apiKeyHash;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("A aplicação já está inativa.");

        Ativo = false;
    }

    public void Reativar()
    {
        if (Ativo)
            throw new DomainException("A aplicação já está ativa.");

        Ativo = true;
    }

    public void AtualizarApiKey(string novoApiKeyHash)
    {
        if (string.IsNullOrWhiteSpace(novoApiKeyHash))
            throw new DomainException("O novo hash da API Key é obrigatório.");

        ApiKeyHash = novoApiKeyHash;
    }
}