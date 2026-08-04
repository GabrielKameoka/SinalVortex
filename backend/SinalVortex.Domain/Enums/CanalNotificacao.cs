namespace SinalVortex.Domain.Enums;

public enum CanalNotificacao
{
    Email = 1,
    WhatsApp = 2,
    Sms = 3,
    Webhook = 4,
    Push = 5
}

public enum PrioridadeNotificacao
{
    Baixa = 1,
    Normal = 2,
    Alta = 3
}

public enum StatusNotificacao
{
    Pendente = 1,
    EmProcessamento = 2,
    Enviado = 3,
    Falhou = 4,
    Dlq = 5
}