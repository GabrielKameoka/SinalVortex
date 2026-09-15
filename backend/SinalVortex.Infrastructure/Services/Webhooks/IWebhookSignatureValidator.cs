namespace SinalVortex.Infrastructure.Services.Webhooks;

public interface IWebhookSignatureValidator
{
    bool ValidarAssinatura(string payload, string assinatura, string secret);
}