using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.Entities;

public class Template
{
    public Guid Id { get; private set; }
    public Guid AplicacaoId { get; private set; }
    public string Chave { get; private set; }
    public string Conteudo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor EF Core
    private Template() { }

    public Template(Guid aplicacaoId, string chave, string conteudo)
    {
        if (aplicacaoId == Guid.Empty)
            throw new DomainException("AplicacaoId inválido.");

        if (string.IsNullOrWhiteSpace(chave))
            throw new DomainException("A chave do template não pode ser vazia.");

        if (string.IsNullOrWhiteSpace(conteudo))
            throw new DomainException("O conteúdo do template não pode ser vazio.");

        Id = Guid.NewGuid();
        AplicacaoId = aplicacaoId;
        Chave = chave.Trim().ToLowerInvariant();
        Conteudo = conteudo;
        CriadoEm = DateTime.UtcNow;
    }

    public void AtualizarConteudo(string novoConteudo)
    {
        if (string.IsNullOrWhiteSpace(novoConteudo))
            throw new DomainException("O conteúdo do template não pode ser vazio.");

        Conteudo = novoConteudo;
    }
}