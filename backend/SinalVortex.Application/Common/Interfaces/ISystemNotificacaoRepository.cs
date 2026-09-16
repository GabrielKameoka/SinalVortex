namespace SinalVortex.Application.Common.Interfaces;

/// <summary>
/// Operações cross-tenant autorizadas exclusivamente para processos de sistema.
/// Esta interface nunca é registrada no host HTTP.
/// </summary>
public interface ISystemNotificacaoRepository
{
    Task<int> RemoverNotificacoesAntigasAsync(DateTime dataCorte, CancellationToken cancellationToken = default);
}
