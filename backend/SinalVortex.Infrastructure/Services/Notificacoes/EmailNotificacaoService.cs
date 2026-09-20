using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace SinalVortex.Infrastructure.Services.Notificacoes;

public sealed class EmailNotificacaoService(IConfiguration configuration) : INotificacaoService
{
    public CanalNotificacao Canal => CanalNotificacao.Email;

    public async Task EnviarAsync(NotificacaoFilaItemDto item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var host = configuration["EmailSettings:SmtpHost"];
        var from = configuration["EmailSettings:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new PermanentChannelException("SMTP não configurado: informe EmailSettings:SmtpHost e EmailSettings:From.");
        using var message = new MailMessage();
        try
        {
            message.From = new MailAddress(from);
            message.To.Add(new MailAddress(item.Destinatario));
        }
        catch (FormatException)
        {
            throw new PermanentChannelException("Endereço de e-mail do remetente ou destinatário inválido.");
        }
        message.Subject = item.Assunto ?? string.Empty;
        message.Body = item.Conteudo;
        message.IsBodyHtml = false;
        using var smtp = new SmtpClient(host, configuration.GetValue("EmailSettings:SmtpPort", 587))
        {
            EnableSsl = configuration.GetValue("EmailSettings:EnableSsl", true),
            UseDefaultCredentials = false
        };
        var username = configuration["EmailSettings:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            smtp.Credentials = new NetworkCredential(username, configuration["EmailSettings:Password"]);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await smtp.SendMailAsync(message, timeout.Token);
        }
        catch (SmtpException exception) when ((int)exception.StatusCode >= 500)
        {
            throw new PermanentChannelException($"SMTP recusou o envio ({exception.StatusCode}).");
        }
    }
}
