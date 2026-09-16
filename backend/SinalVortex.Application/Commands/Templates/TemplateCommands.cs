using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Models;

namespace SinalVortex.Application.Commands.Templates;

public sealed record CriarTemplateCommand(Guid AplicacaoId, string Chave, string Conteudo) : IRequest<TemplateDto>;
public sealed record AtualizarTemplateCommand(Guid Id, string Conteudo) : IRequest<bool>;
public sealed record RemoverTemplateCommand(Guid Id) : IRequest<bool>;
public sealed record ObterTemplatesQuery(string? Busca = null, int PageNumber = 1, int PageSize = 20) : IRequest<TemplatePageDto>;
public sealed record ObterTemplateQuery(Guid Id) : IRequest<TemplateDto?>;
public sealed record TemplateDto(Guid Id, Guid AplicacaoId, string Chave, string Conteudo, DateTime CriadoEm);
public sealed record TemplatePageDto(IReadOnlyCollection<TemplateDto> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasNextPage, bool HasPreviousPage);

public sealed class CriarTemplateHandler(ITemplateRepository repository, ITenantContext tenantContext) : IRequestHandler<CriarTemplateCommand, TemplateDto>
{
    public async Task<TemplateDto> Handle(CriarTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.AplicacaoPertenceAoTenantAsync(request.AplicacaoId, cancellationToken))
            throw new KeyNotFoundException("Aplicação não encontrada para o tenant atual.");
        var template = new Template(tenantContext.TenantId, request.AplicacaoId, request.Chave, request.Conteudo);
        await repository.AdicionarAsync(template, cancellationToken);
        return ToDto(template);
    }
    internal static TemplateDto ToDto(Template t) => new(t.Id, t.AplicacaoId, t.Chave, t.Conteudo, t.CriadoEm);
}

public sealed class AtualizarTemplateHandler(ITemplateRepository repository) : IRequestHandler<AtualizarTemplateCommand, bool>
{
    public async Task<bool> Handle(AtualizarTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (template is null) return false;
        template.AtualizarConteudo(request.Conteudo);
        await repository.AtualizarAsync(template, cancellationToken);
        return true;
    }
}

public sealed class RemoverTemplateHandler(ITemplateRepository repository) : IRequestHandler<RemoverTemplateCommand, bool>
{
    public async Task<bool> Handle(RemoverTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (template is null) return false;
        await repository.RemoverAsync(template, cancellationToken);
        return true;
    }
}

public sealed class ObterTemplatesHandler(ITemplateRepository repository) : IRequestHandler<ObterTemplatesQuery, TemplatePageDto>
{
    public async Task<TemplatePageDto> Handle(ObterTemplatesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.ObterPaginadoAsync(request.Busca, request.PageNumber, request.PageSize, cancellationToken);
        var pages = (int)Math.Ceiling(total / (double)request.PageSize);
        return new(items.Select(CriarTemplateHandler.ToDto).ToArray(), request.PageNumber, request.PageSize, total, pages, request.PageNumber < pages, request.PageNumber > 1);
    }
}

public sealed class ObterTemplateHandler(ITemplateRepository repository) : IRequestHandler<ObterTemplateQuery, TemplateDto?>
{
    public async Task<TemplateDto?> Handle(ObterTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        return template is null ? null : CriarTemplateHandler.ToDto(template);
    }
}
