namespace SinalVortex.Application.Commands.Contatos;

using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Enums;

public record CriarContatoCRMCommand(
    string Nome,
    string Email,
    string Telefone,
    CanalNotificacao CanalPreferencial,
    IReadOnlyCollection<string>? Tags = null
) : IRequest<Guid>;

public class CriarContatoCRMCommandHandler : IRequestHandler<CriarContatoCRMCommand, Guid>
{
    private readonly IContatoRepository _contatoRepository;
    private readonly ITenantContext _tenantContext;

    public CriarContatoCRMCommandHandler(
        IContatoRepository contatoRepository,
        ITenantContext tenantContext)
    {
        _contatoRepository = contatoRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> Handle(CriarContatoCRMCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtém o TenantId do contexto ativo na requisição
        var tenantId = _tenantContext.TenantId;

        // 2. Instancia a entidade repassando apenas os parâmetros suportados pelo construtor
        var contato = new Contato(
            tenantId,
            request.Nome,
            request.Email,
            request.Telefone
        );

        // 3. Define o canal preferencial via método de domínio
        contato.DefinirCanalPreferencial(request.CanalPreferencial);
        if (request.Tags is not null)
            foreach (var tag in request.Tags)
                contato.AdicionarTag(tag);

        await _contatoRepository.AdicionarAsync(contato, cancellationToken);

        return contato.Id;
    }
}
