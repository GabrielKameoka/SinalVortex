using System.Text.RegularExpressions;
using SinalVortex.Domain.Common;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.ValueObjects;

public sealed class Destinatario : ValueObject
{
    public string Valor { get; private set; }

    private Destinatario(string valor)
    {
        Valor = valor;
    }

    public static Destinatario Criar(string valor, CanalNotificacao canal)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("O destinatário não pode ser vazio.");

        valor = valor.Trim();

        switch (canal)
        {
            case CanalNotificacao.Email:
                ValidarEmail(valor);
                break;
            case CanalNotificacao.WhatsApp:
            case CanalNotificacao.Sms:
                valor = ValidarETratarTelefone(valor);
                break;
            case CanalNotificacao.Webhook:
                ValidarUrl(valor);
                break;
            case CanalNotificacao.Push:
                // Device token / Push ID - Apenas valida tamanho mínimo
                if (valor.Length < 10)
                    throw new DomainException("Token de Push Notification inválido.");
                break;
        }

        return new Destinatario(valor);
    }

    private static void ValidarEmail(string email)
    {
        var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        if (!regex.IsMatch(email))
            throw new DomainException($"Endereço de e-mail inválido: '{email}'");
    }

    private static string ValidarETratarTelefone(string telefone)
    {
        var ApenasNumeros = Regex.Replace(telefone, @"[^\d]", "");
        if (ApenasNumeros.Length < 10 || ApenasNumeros.Length > 15)
            throw new DomainException($"Número de telefone/WhatsApp inválido: '{telefone}'");

        return ApenasNumeros;
    }

    private static void ValidarUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException($"URL de Webhook inválida: '{url}'");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;
}