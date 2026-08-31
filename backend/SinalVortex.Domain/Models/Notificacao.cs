using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using SinalVortex.Domain.ValueObjects;

namespace SinalVortex.Domain.Models;

public class Notificacao
{
    private readonly List<LogNotificacao> _logs = new();

    public Guid Id { get; private set; }
    public Guid AplicacaoId { get; private set; }
    public Destinatario Destinatario { get; private set; }
    public CanalNotificacao Canal { get; private set; }
    public PrioridadeNotificacao Prioridade { get; private set; }
    public StatusNotificacao Status { get; private set; }
    public string Conteudo { get; private set; }
    public string? Assunto { get; private set; }
    public Guid? TemplateId { get; private set; }
    public int Tentativas { get; private set; }
    public int MaxTentativas { get; private set; }
    public DateTime? AgendadoPara { get; private set; }
    public DateTime? ProcessadoEm { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public IReadOnlyCollection<LogNotificacao> Logs => _logs.AsReadOnly();

    private Notificacao() { }

    public Notificacao(
        Guid aplicacaoId,
        Destinatario destinatario,
        CanalNotificacao canal,
        PrioridadeNotificacao prioridade,
        string conteudo,
        string? assunto = null,
        Guid? templateId = null,
        int maxTentativas = 3,
        DateTime? agendadoPara = null)
    {
        if (aplicacaoId == Guid.Empty)
            throw new DomainException("AplicacaoId é obrigatório.");

        if (string.IsNullOrWhiteSpace(conteudo))
            throw new DomainException("O conteúdo da notificação não pode ser vazio.");

        if (maxTentativas <= 0)
            throw new DomainException("MaxTentativas deve ser maior que zero.");

        Id = Guid.NewGuid();
        AplicacaoId = aplicacaoId;
        Destinatario = destinatario;
        Canal = canal;
        Prioridade = prioridade;
        Status = StatusNotificacao.Pendente;
        Conteudo = conteudo;
        Assunto = assunto;
        TemplateId = templateId;
        Tentativas = 0;
        MaxTentativas = maxTentativas;
        AgendadoPara = agendadoPara;
        CriadoEm = DateTime.UtcNow;

        AdicionarLog(StatusNotificacao.Pendente, StatusNotificacao.Pendente, "Notificação criada e enfileirada.");
    }

    // --- Métodos de Comportamento de Domínio Rico ---

    public void IniciarProcessamento()
    {
        if (Status == StatusNotificacao.Enviado)
            throw new DomainException("Uma notificação já enviada não pode ser reprocessada.");

        if (Status == StatusNotificacao.Dlq)
            throw new DomainException("Notificação em DLQ requer intervenção antes do reprocessamento.");

        var statusAnterior = Status;
        Status = StatusNotificacao.EmProcessamento;
        Tentativas++;

        AdicionarLog(statusAnterior, StatusNotificacao.EmProcessamento, $"Iniciada tentativa {Tentativas} de {MaxTentativas}.");
    }

    public void MarcarComoEnviado()
    {
        if (Status != StatusNotificacao.EmProcessamento)
            throw new DomainException("A notificação precisa estar EmProcessamento para ser marcada como Enviada.");

        var statusAnterior = Status;
        Status = StatusNotificacao.Enviado;
        ProcessadoEm = DateTime.UtcNow;

        AdicionarLog(statusAnterior, StatusNotificacao.Enviado, "Mensagem enviada com sucesso ao provedor.");
    }

    public void RegistrarFalha(string motivo)
    {
        if (Status != StatusNotificacao.EmProcessamento)
            throw new DomainException("Apenas notificações em processamento podem registrar falhas.");

        var statusAnterior = Status;
        var mensagemErro = string.IsNullOrWhiteSpace(motivo) ? "Falha não identificada no envio." : motivo;

        if (Tentativas >= MaxTentativas)
        {
            EnviarParaDlq(mensagemErro);
        }
        else
        {
            Status = StatusNotificacao.Falhou;
            AdicionarLog(statusAnterior, StatusNotificacao.Falhou, $"Falha na tentativa {Tentativas}/{MaxTentativas}: {mensagemErro}");
        }
    }

    public void EnviarParaDlq(string motivo)
    {
        var statusAnterior = Status;
        Status = StatusNotificacao.Dlq;
        ProcessadoEm = DateTime.UtcNow;

        AdicionarLog(statusAnterior, StatusNotificacao.Dlq, $"Movido para Dead-Letter Queue (DLQ). Motivo: {motivo}");
    }

    public void ReprocessarAposDlq()
    {
        if (Status != StatusNotificacao.Dlq)
            throw new DomainException("Apenas notificações em DLQ podem ser solicitadas para reprocessamento manual.");

        var statusAnterior = Status;
        Status = StatusNotificacao.Pendente;
        Tentativas = 0; // Reseta tentativas para novo ciclo

        AdicionarLog(statusAnterior, StatusNotificacao.Pendente, "Reprocessamento manual solicitado via painel.");
    }

    private void AdicionarLog(StatusNotificacao anterior, StatusNotificacao novo, string? mensagem = null)
    {
        _logs.Add(new LogNotificacao(Id, anterior, novo, mensagem));
    }
}