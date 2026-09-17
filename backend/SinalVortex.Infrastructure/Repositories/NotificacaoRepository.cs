using System.Linq;
using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Models;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Application.Queries.Dashboard;

namespace SinalVortex.Infrastructure.Repositories;

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
            .Where(n => n.CreatedAt < dataCorte && (n.Status == StatusNotificacao.Enviado || n.Status == StatusNotificacao.Dlq))
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
        IQueryable<Notificacao> query = context.Notificacoes.AsNoTracking();

        if (aplicacaoId.HasValue)
            query = query.Where(n => n.AplicacaoId == aplicacaoId.Value);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        if (canal.HasValue)
            query = query.Where(n => n.Canal == canal.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<DashboardMetricsData> ObterMetricasDashboardAsync(
        DateTime dataInicialUtc,
        CancellationToken cancellationToken = default)
    {
        var query = context.Notificacoes
            .AsNoTracking()
            .Where(n => n.CreatedAt >= dataInicialUtc);

        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Delivered = group.Count(n => n.Status == StatusNotificacao.Enviado),
                FailedOrDlq = group.Count(n => n.Status == StatusNotificacao.Falhou || n.Status == StatusNotificacao.Dlq)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var byChannel = await query
            .GroupBy(n => n.Canal)
            .Select(group => new { Channel = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Channel, item => item.Count, cancellationToken);

        return new DashboardMetricsData(
            summary?.Total ?? 0,
            summary?.Delivered ?? 0,
            summary?.FailedOrDlq ?? 0,
            byChannel);
    }
}
