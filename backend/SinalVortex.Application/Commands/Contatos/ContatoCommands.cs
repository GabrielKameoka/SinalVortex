using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Enums;

namespace SinalVortex.Application.Commands.Contatos;

public sealed record AtualizarContatoCommand(Guid Id, string Nome, string? Email, string? Telefone, CanalNotificacao CanalPreferencial) : IRequest<bool>;
public sealed record RemoverContatoCommand(Guid Id) : IRequest<bool>;

public sealed class AtualizarContatoHandler(IContatoRepository repository) : IRequestHandler<AtualizarContatoCommand, bool>
{
    public async Task<bool> Handle(AtualizarContatoCommand request, CancellationToken cancellationToken)
    {
        var contato = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (contato is null) return false;
        contato.Atualizar(request.Nome, request.Email, request.Telefone, request.CanalPreferencial);
        await repository.AtualizarAsync(contato, cancellationToken);
        return true;
    }
}

public sealed class RemoverContatoHandler(IContatoRepository repository) : IRequestHandler<RemoverContatoCommand, bool>
{
    public async Task<bool> Handle(RemoverContatoCommand request, CancellationToken cancellationToken)
    {
        var contato = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (contato is null) return false;
        await repository.RemoverAsync(contato, cancellationToken);
        return true;
    }
}

public sealed record ObterContatosQuery(string? Busca = null, int PageNumber = 1, int PageSize = 20) : IRequest<ContatoPageDto>;
public sealed record ObterContatoQuery(Guid Id) : IRequest<ContatoDto?>;
public sealed record ContatoDto(Guid Id, string Nome, string? Email, string? Telefone, CanalNotificacao CanalPreferencial, IReadOnlyCollection<string> Tags, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record ContatoPageDto(IReadOnlyCollection<ContatoDto> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasNextPage, bool HasPreviousPage);

public sealed class ObterContatosHandler(IContatoRepository repository) : IRequestHandler<ObterContatosQuery, ContatoPageDto>
{
    public async Task<ContatoPageDto> Handle(ObterContatosQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.ObterPaginadoAsync(request.Busca, request.PageNumber, request.PageSize, cancellationToken);
        var pages = (int)Math.Ceiling(total / (double)request.PageSize);
        return new(items.Select(c => new ContatoDto(c.Id, c.Nome, c.Email, c.Telefone, c.CanalPreferencial, c.Tags, c.CreatedAt, c.UpdatedAt)).ToArray(), request.PageNumber, request.PageSize, total, pages, request.PageNumber < pages, request.PageNumber > 1);
    }
}

public sealed class ObterContatoHandler(IContatoRepository repository) : IRequestHandler<ObterContatoQuery, ContatoDto?>
{
    public async Task<ContatoDto?> Handle(ObterContatoQuery request, CancellationToken cancellationToken)
    {
        var c = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        return c is null ? null : new(c.Id, c.Nome, c.Email, c.Telefone, c.CanalPreferencial, c.Tags, c.CreatedAt, c.UpdatedAt);
    }
}
