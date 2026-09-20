using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using SinalVortex.Infrastructure.Services.Notificacoes;

namespace SinalVortex.IntegrationTests;

public class RealDeliveryTests
{
    private static NotificacaoFilaItemDto Item(CanalNotificacao channel, string recipient) =>
        new(Guid.NewGuid(), Guid.NewGuid(), channel, PrioridadeNotificacao.Normal, recipient, "Mensagem real de teste", "Assunto", Guid.NewGuid());

    [Fact]
    public async Task Email_DeveTransmitirMensagemPorSmtp()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var received = ReceiveMailAsync(listener, timeout.Token);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EmailSettings:SmtpHost"] = "127.0.0.1",
            ["EmailSettings:SmtpPort"] = ((IPEndPoint)listener.LocalEndpoint).Port.ToString(),
            ["EmailSettings:EnableSsl"] = "false",
            ["EmailSettings:From"] = "sender@example.test"
        }).Build();
        await new EmailNotificacaoService(configuration).EnviarAsync(Item(CanalNotificacao.Email, "recipient@example.test"), timeout.Token);
        var content = await received;
        Assert.Contains("recipient@example.test", content);
        Assert.Contains("Assunto", content);
    }

    [Fact]
    public async Task Email_SemConfiguracao_NaoDeveSimularSucesso()
    {
        await Assert.ThrowsAsync<PermanentChannelException>(() => new EmailNotificacaoService(new ConfigurationBuilder().Build())
            .EnviarAsync(Item(CanalNotificacao.Email, "recipient@example.test"), CancellationToken.None));
    }

    [Theory]
    [InlineData(200, null)]
    [InlineData(400, typeof(PermanentChannelException))]
    [InlineData(302, typeof(PermanentChannelException))]
    [InlineData(429, typeof(TransientChannelException))]
    [InlineData(503, typeof(TransientChannelException))]
    public async Task Webhook_DeveUsarRespostaHttpReal(int status, Type? errorType)
    {
        var handler = new RecordingHandler((HttpStatusCode)status);
        using var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("notification-webhook").Returns(client);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["WebhookSettings:AllowedOrigins:0"] = "https://receiver.example.test" }).Build();
        var item = Item(CanalNotificacao.Webhook, "https://receiver.example.test/events");
        var exception = await Record.ExceptionAsync(() => new WebhookNotificacaoService(factory, configuration).EnviarAsync(item, CancellationToken.None));
        if (errorType is null) Assert.Null(exception); else Assert.IsType(errorType, exception);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Contains(item.NotificacaoId.ToString(), handler.Body);
        Assert.Equal(item.NotificacaoId.ToString(), handler.IdempotencyKey);
    }

    [Fact]
    public async Task Webhook_DestinoNaoAutorizado_NaoDeveEnviar()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        await Assert.ThrowsAsync<PermanentChannelException>(() => new WebhookNotificacaoService(factory, new ConfigurationBuilder().Build())
            .EnviarAsync(Item(CanalNotificacao.Webhook, "https://unapproved.example.test"), CancellationToken.None));
        factory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string Body { get; private set; } = "";
        public string? IdempotencyKey { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            IdempotencyKey = request.Headers.GetValues("Idempotency-Key").Single();
            return new HttpResponseMessage(status);
        }
    }

    private static async Task<string> ReceiveMailAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var socket = await listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = socket.GetStream();
        using var reader = new StreamReader(stream);
        await using var writer = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };
        await writer.WriteLineAsync("220 localhost SMTP test");
        var content = new StringBuilder();
        var data = false;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (data)
            {
                if (line == ".") { await writer.WriteLineAsync("250 accepted"); return content.ToString(); }
                content.AppendLine(line);
            }
            else if (line == "DATA") { data = true; await writer.WriteLineAsync("354 send message"); }
            else await writer.WriteLineAsync("250 OK");
        }
        throw new InvalidOperationException("SMTP connection closed without DATA.");
    }
}
