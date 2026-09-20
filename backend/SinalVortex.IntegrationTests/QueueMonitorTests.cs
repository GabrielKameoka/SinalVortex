using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SinalVortex.Worker;
using SinalVortex.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.IntegrationTests;

public class QueueMonitorTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PostNotificacao_DeveEntregarConsumoESucessoPeloSignalR()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var client = factory.CreateClient();
        using var socket = await ConnectAsync(deadline.Token);
        var response = await client.PostAsJsonAsync("/api/v1/Notificacoes", new
        {
            aplicacaoId = Guid.NewGuid(), destinatario = "monitor@example.test",
            canal = 1, prioridade = 1, assunto = "Teste do monitor",
            conteudo = "Evento de teste em infraestrutura isolada."
        }, deadline.Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(deadline.Token);
        var id = body.GetProperty("id").GetString()!;

        var logs = new List<JsonElement>();
        while (!logs.Any(log => log.GetProperty("level").GetString() == "success"))
        {
            var frame = await ReceiveAsync(socket, deadline.Token);
            if (!frame.TryGetProperty("target", out var target) || target.GetString() != "queueUpdate") continue;
            var log = frame.GetProperty("arguments")[0];
            if (log.GetProperty("message").GetString()!.Contains(id)) logs.Add(log.Clone());
        }

        Assert.Contains(logs, log => log.GetProperty("level").GetString() == "info");
        Assert.All(logs, log =>
        {
            Assert.Equal("11111111-1111-1111-1111-111111111111", log.GetProperty("tenantId").GetString());
            foreach (var queue in new[] { "high", "normal", "low", "dlq" })
                Assert.True(log.GetProperty("queues").GetProperty(queue).GetInt32() >= 0);
        });
    }

    [Fact]
    public async Task EventoDeOutroTenant_NaoDeveChegarNaConexao()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var client = factory.CreateClient();
        using var socket = await ConnectAsync(deadline.Token);
        var publisher = factory.Services.GetRequiredService<QueueMonitorPublisher>();
        await publisher.PublishAsync(Guid.NewGuid(), "info", "outro-tenant");
        await publisher.PublishAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"), "info", "tenant-correto");
        while (true)
        {
            var frame = await ReceiveAsync(socket, deadline.Token);
            if (!frame.TryGetProperty("target", out var target) || target.GetString() != "queueUpdate") continue;
            Assert.Equal("tenant-correto", frame.GetProperty("arguments")[0].GetProperty("message").GetString());
            break;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FalhaNoProvedor_DevePublicarDlqESomenteRetentarFalhasTransitorias(bool permanent)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var client = factory.CreateClient();
        using var socket = await ConnectAsync(deadline.Token);
        var recipient = $"failure-{Guid.NewGuid():N}@example.test";
        var provider = factory.Services.GetServices<INotificacaoService>().Single(service => service.Canal == CanalNotificacao.Email);
        provider.EnviarAsync(Arg.Is<NotificacaoFilaItemDto>(item => item.Destinatario == recipient), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(permanent
                ? new PermanentChannelException("Provedor recusou o envio de teste.")
                : new TimeoutException("Timeout de teste.")));

        var response = await client.PostAsJsonAsync("/api/v1/Notificacoes", new
        {
            aplicacaoId = Guid.NewGuid(), destinatario = recipient, canal = 1,
            prioridade = 2, assunto = "Teste DLQ", conteudo = "Falha controlada no provedor de teste"
        }, deadline.Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(deadline.Token);
        var id = body.GetProperty("id").GetString()!;
        var logs = new List<JsonElement>();
        while (!logs.Any(log => log.GetProperty("level").GetString() == "error"))
        {
            var frame = await ReceiveAsync(socket, deadline.Token);
            if (!frame.TryGetProperty("target", out var target) || target.GetString() != "queueUpdate") continue;
            var log = frame.GetProperty("arguments")[0];
            if (log.GetProperty("message").GetString()!.Contains(id)) logs.Add(log.Clone());
        }
        Assert.DoesNotContain(logs, log => log.GetProperty("level").GetString() == "success");
        Assert.Equal(!permanent, logs.Any(log => log.GetProperty("level").GetString() == "warning"));
        Assert.True(logs.Last().GetProperty("queues").GetProperty("dlq").GetInt32() > 0);
        var notification = await client.GetFromJsonAsync<JsonElement>($"/api/v1/Notificacoes/{id}", deadline.Token);
        Assert.Equal((int)StatusNotificacao.Dlq, notification.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task PostSemWorker_DevePublicarEntradaEContagemReal()
    {
        await using var isolated = new CustomWebApplicationFactory();
        await isolated.InitializeAsync();
        try
        {
            await using var host = isolated.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                var worker = services.Single(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(SignalProcessingWorker));
                services.Remove(worker);
            }));
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var client = host.CreateClient();
            using var socket = await ConnectAsync(deadline.Token, host);
            var response = await client.PostAsJsonAsync("/api/v1/Notificacoes", new
            {
                aplicacaoId = Guid.NewGuid(), destinatario = "queued@example.test",
                canal = 1, prioridade = 3, assunto = "Teste de fila", conteudo = "Sem Worker ativo"
            }, deadline.Token);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var frame = await ReceiveAsync(socket, deadline.Token);
            Assert.Equal("queueUpdate", frame.GetProperty("target").GetString());
            var update = frame.GetProperty("arguments")[0];
            Assert.Contains("adicionada à fila alta", update.GetProperty("message").GetString());
            Assert.Equal(1, update.GetProperty("queues").GetProperty("high").GetInt32());
        }
        finally { await isolated.DisposeAsync(); }
    }

    private async Task<WebSocket> ConnectAsync(CancellationToken cancellationToken, WebApplicationFactory<Program>? host = null)
    {
        var socket = await (host ?? factory).Server.CreateWebSocketClient()
            .ConnectAsync(new Uri("ws://localhost/hubs/queue-monitor"), cancellationToken);
        await SendAsync(socket, "{\"protocol\":\"json\",\"version\":1}", cancellationToken);
        var handshake = await ReceiveAsync(socket, cancellationToken);
        Assert.False(handshake.TryGetProperty("error", out _));
        await SendAsync(socket, "{\"type\":1,\"invocationId\":\"snapshot\",\"target\":\"GetSnapshot\",\"arguments\":[]}", cancellationToken);
        var snapshot = await ReceiveAsync(socket, cancellationToken);
        Assert.True(snapshot.TryGetProperty("result", out _), snapshot.ToString());
        return socket;
    }

    private static Task SendAsync(WebSocket socket, string json, CancellationToken cancellationToken) =>
        socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json + '\u001e')), WebSocketMessageType.Text, true, cancellationToken);

    // Read one protocol frame at a time, including when several share a WebSocket message.
    private static async Task<JsonElement> ReceiveAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>();
        var buffer = new byte[1];
        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            Assert.NotEqual(WebSocketMessageType.Close, result.MessageType);
            if (result.Count == 0) continue;
            if (buffer[0] == 0x1e) return JsonSerializer.Deserialize<JsonElement>(bytes.ToArray());
            bytes.Add(buffer[0]);
        }
    }
}
