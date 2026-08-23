using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Models;
using SinalVortex.Infrastructure.Persistence;

namespace SinalVortex.Infrastructure.Repositories;

public class NotificacaoRepository : INotificacaoRepository
{
    private readonly AppDbContext _context;

    public NotificacaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Notificacao notificacao, CancellationToken cancellationToken = default)
    {
        await _context.Notificacoes.AddAsync(notificacao, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Notificacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notificacoes
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }
    
    public async Task<int> RemoverNotificacoesAntigasAsync(DateTime dataCorte, CancellationToken cancellationToken = default)
    {
        // Remove diretamente do PostgreSQL as notificações já concluídas (Enviado ou Dlq) criadas há mais de 30 dias
        return await _context.Notificacoes
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
        var query = _context.Notificacoes.AsNoTracking();

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