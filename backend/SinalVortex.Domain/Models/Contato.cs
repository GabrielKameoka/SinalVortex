using SinalVortex.Domain.Common;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.Entities;

public class Contato : BaseEntity
{
    public string Nome { get; private set; }
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public CanalNotificacao CanalPreferencial { get; private set; }

    private readonly List<string> _tags = new();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    private Contato() { }

    public Contato(Guid tenantId, string nome, string? email = null, string? telefone = null)
        : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do contato é obrigatório.");

        Nome = nome.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Telefone = telefone?.Trim();
        CanalPreferencial = CanalNotificacao.Email;
    }

    public void DefinirCanalPreferencial(CanalNotificacao canal)
    {
        CanalPreferencial = canal;
        Touch();
    }

    public void Atualizar(string nome, string? email, string? telefone, CanalNotificacao canal)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do contato é obrigatório.");
        Nome = nome.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Telefone = telefone?.Trim();
        CanalPreferencial = canal;
        Touch();
    }

    public void AdicionarTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        var tagFormatada = tag.Trim().ToLowerInvariant();
        if (!_tags.Contains(tagFormatada))
        {
            _tags.Add(tagFormatada);
            Touch();
        }
    }

    public void RemoverTag(string tag)
    {
        var tagFormatada = tag.Trim().ToLowerInvariant();
        if (_tags.Remove(tagFormatada))
        {
            Touch();
        }
    }
}
