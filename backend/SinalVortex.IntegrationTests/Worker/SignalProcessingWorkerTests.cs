using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Services.Notificacoes;
using SinalVortex.IntegrationTests.Support;
using StackExchange.Redis;

namespace SinalVortex.IntegrationTests.Worker;

public class SignalProcessingWorkerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData(250, PrioridadeNotificacao.Baixa, false, CanalNotificacao.Email, StatusNotificacao.Enviado, 1)]
    [InlineData(250, PrioridadeNotificacao.Normal, false, CanalNotificacao.Email, StatusNotificacao.Enviado, 1)]
    [InlineData(250, PrioridadeNotificacao.Alta, false, CanalNotificacao.Email, StatusNotificacao.Enviado, 1)]
    [InlineData(250, PrioridadeNotificacao.Normal, true, CanalNotificacao.Email, StatusNotificacao.Dlq, 1)]
    [InlineData(451, PrioridadeNotificacao.Normal, false, CanalNotificacao.Email, StatusNotificacao.Dlq, 3)]
    [InlineData(550, PrioridadeNotificacao.Normal, false, CanalNotificacao.Email, StatusNotificacao.Dlq, 1)]
    [InlineData(250, PrioridadeNotificacao.Normal, false, CanalNotificacao.WhatsApp, StatusNotificacao.Dlq, 1)]
    public async Task Post_DeveProcessarComWorkerEProvedorReais(
        int smtpStatus, PrioridadeNotificacao priority, bool missingConfiguration,
        CanalNotificacao channel, StatusNotificacao expectedStatus, int expectedAttempts)
    {
        await using var smtp = new SmtpTestServer(smtpStatus);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var settings = new Dictionary<string, string?>();
        if (!missingConfiguration)
        {
            settings["EmailSettings:SmtpHost"] = "127.0.0.1";
            settings["EmailSettings:SmtpPort"] = smtp.Port.ToString();
            settings["EmailSettings:EnableSsl"] = "false";
            settings["EmailSettings:From"] = "sender@example.test";
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<INotificacaoService>();
            services.AddScoped<INotificacaoService>(_ => new EmailNotificacaoService(configuration));
            services.AddScoped<INotificacaoService, WhatsappNotificacaoService>();
        }));
        using var client = host.CreateClient();
        var recipient = channel == CanalNotificacao.Email ? $"worker-{Guid.NewGuid():N}@example.test" : "+5511999999999";
        var subject = $"SMTP integration {Guid.NewGuid():N}";
        const string content = "Message transmitted through the real Worker and SMTP transport.";
        using var response = await client.PostAsJsonAsync("/api/v1/notificacoes", new
        {
            aplicacaoId = Guid.NewGuid(), destinatario = recipient, canal = channel,
            prioridade = priority, assunto = subject, conteudo = content
        }, deadline.Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(deadline.Token);
        var id = created.GetProperty("id").GetGuid();

        JsonElement notification;
        do
        {
            notification = await client.GetFromJsonAsync<JsonElement>($"/api/v1/notificacoes/{id}", deadline.Token);
            if (notification.GetProperty("status").GetInt32() is (int)StatusNotificacao.Enviado or (int)StatusNotificacao.Dlq) break;
            await Task.Delay(50, deadline.Token);
        } while (true);

        Assert.Equal((int)expectedStatus, notification.GetProperty("status").GetInt32());
        Assert.Equal(expectedAttempts, notification.GetProperty("tentativas").GetInt32());
        Assert.Equal(missingConfiguration || channel == CanalNotificacao.WhatsApp ? 0 : expectedAttempts, smtp.Attempts);

        var redis = host.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        bool inDlq;
        do
        {
            var entries = await redis.ListRangeAsync("notificacoes:fila:dlq");
            inDlq = entries.Any(entry => JsonSerializer.Deserialize<NotificacaoFilaItemDto>(entry.ToString())!.NotificacaoId == id);
            if (expectedStatus != StatusNotificacao.Dlq || inDlq) break;
            await Task.Delay(50, deadline.Token);
        } while (true);
        Assert.Equal(expectedStatus == StatusNotificacao.Dlq, inDlq);

        if (expectedStatus == StatusNotificacao.Enviado)
        {
            var message = await smtp.Message.WaitAsync(deadline.Token);
            Assert.Contains(recipient, message);
            Assert.Contains(subject, message);
            Assert.Contains(content, message);
        }
        else
        {
            Assert.False(smtp.Message.IsCompletedSuccessfully);
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var saved = await db.Notificacoes.AsNoTracking().Include(n => n.Logs).SingleAsync(n => n.Id == id, deadline.Token);
            var reason = missingConfiguration ? "SMTP não configurado"
                : channel == CanalNotificacao.WhatsApp ? "Envio de WhatsApp indisponível"
                : smtpStatus == 550 ? "SMTP recusou o envio" : "recipient response";
            Assert.Contains(saved.Logs, log => log.NovoStatus == StatusNotificacao.Dlq && log.MensagemErro!.Contains(reason));
        }
    }
}
