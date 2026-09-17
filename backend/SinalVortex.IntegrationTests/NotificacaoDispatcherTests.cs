using Microsoft.Extensions.DependencyInjection;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;
using Xunit;

namespace SinalVortex.IntegrationTests.Services;

public class NotificacaoDispatcherIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificacaoDispatcherIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnviarAsync_ComCanalEmail_DeveDespacharSemLancarExcecao()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificacaoDispatcher>();

        var item = new NotificacaoFilaItemDto(
            NotificacaoId: Guid.NewGuid(),
            AplicacaoId: Guid.NewGuid(),
            Canal: CanalNotificacao.Email,
            Prioridade: PrioridadeNotificacao.Normal,
            Destinatario: "dev@sinalvortex.com",
            Conteudo: "Validando execução do driver de Email via Dispatcher no IoC.",
            Assunto: "Teste do Dispatcher Integration"
        );

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => dispatcher.EnviarAsync(item, CancellationToken.None));
        
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(CanalNotificacao.Email, "dev@sinalvortex.com")]
    [InlineData(CanalNotificacao.Sms, "+5511999999999")]
    [InlineData(CanalNotificacao.WhatsApp, "+5511999999999")]
    [InlineData(CanalNotificacao.WhatsApp, "5511999999999")]
    [InlineData(CanalNotificacao.Webhook, "https://webhook.site/test")]
    [InlineData(CanalNotificacao.Push, "device-token-12345")]
    public async Task EnviarAsync_ParaTodosCanaisRegistrados_DeveResolverDriverEConsumir(
        CanalNotificacao canal, 
        string destinatario)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificacaoDispatcher>();

        var item = new NotificacaoFilaItemDto(
            NotificacaoId: Guid.NewGuid(),
            AplicacaoId: Guid.NewGuid(),
            Canal: canal,
            Prioridade: PrioridadeNotificacao.Alta,
            Destinatario: destinatario,
            Conteudo: $"Testando canal {canal} via integração com contêiner real.",
            Assunto: "Teste Multi-Canal Integration"
        );

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => dispatcher.EnviarAsync(item, CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task EnviarAsync_ComCanalInvalidoOuNaoRegistrado_NaoDeveFalhar()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificacaoDispatcher>();

        var item = new NotificacaoFilaItemDto(
            NotificacaoId: Guid.NewGuid(),
            AplicacaoId: Guid.NewGuid(),
            Canal: (CanalNotificacao)999, // Canal inexistente
            Prioridade: PrioridadeNotificacao.Baixa,
            Destinatario: "teste@sinalvortex.com",
            Conteudo: "Teste de resiliência para canal não registrado.",
            Assunto: null
        );

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => dispatcher.EnviarAsync(item, CancellationToken.None));

        Assert.Null(exception);
    }
}
