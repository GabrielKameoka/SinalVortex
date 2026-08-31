using SinalVortex.Domain.Enums;

namespace SinalVortex.Domain.Entities;

public class LogNotificacao
{
    public long Id { get; private set; }
    public Guid NotificacaoId { get; private set; }
    public StatusNotificacao StatusAnterior { get; private set; }
    public StatusNotificacao NovoStatus { get; private set; }
    public string? MensagemErro { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor EF Core
    private LogNotificacao() { }

    public LogNotificacao(Guid notificacaoId, StatusNotificacao statusAnterior, StatusNotificacao novoStatus, string? mensagemErro = null)
    {
        NotificacaoId = notificacaoId;
        StatusAnterior = statusAnterior;
        NovoStatus = novoStatus;
        MensagemErro = mensagemErro;
        CriadoEm = DateTime.UtcNow;
    }
}