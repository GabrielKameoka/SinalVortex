namespace SinalVortex.Infrastructure.Services.Webhooks;

using System.Security.Cryptography;
using System.Text;

public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    public bool ValidarAssinatura(string payload, string assinatura, string secret)
    {
        if (string.IsNullOrWhiteSpace(assinatura) || string.IsNullOrWhiteSpace(secret))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var hashHex = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(hashHex, assinatura.Replace("sha256=", ""), StringComparison.OrdinalIgnoreCase);
    }
}