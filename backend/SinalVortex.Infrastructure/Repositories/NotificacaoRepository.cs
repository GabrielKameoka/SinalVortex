namespace SinalVortex.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Models;
using SinalVortex.Infrastructure.Persistence;

public class NotificacaoRepository(AppDbContext context) : INotificacaoRepository
{
    public async Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken = default)
    {
        await context.Notificacoes.AddAsync(notificacao, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Notificacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Notificacoes
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task AtualizarAsync(Notificacao notificacao, CancellationToken cancellationToken = default)
    {
        context.Notificacoes.Update(notificacao);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RemoverNotificacoesAntigasAsync(DateTime dataCorte, CancellationToken cancellationToken = default)
    {
        return await context.Notificacoes
            .Where(n => n.CriadoEm < dataCorte && (n.Status == StatusNotificacao.Enviado || n.Status == StatusNotificacao.Dlq))
            .ExecuteDeleteAsync(cancellationToken);
    }
    
    public async Task<(IReadOnlyCollection<Notificacao> Items, int TotalCount)> ObterPaginadoAsync(
        Guid? aplicacaoId,
        StatusNotificacao? status,
        CanalNotificacao? canal,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Notificacoes.AsNoTracking();

        if (aplicacaoId.HasValue)
            query = query.Where(n => n.AplicacaoId == aplicacaoId.Value);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        if (canal.HasValue)
            query = query.Where(n => n.Canal == canal.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CriadoEm)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}