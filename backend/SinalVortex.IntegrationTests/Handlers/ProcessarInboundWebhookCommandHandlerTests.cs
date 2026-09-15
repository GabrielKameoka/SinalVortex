namespace SinalVortex.IntegrationTests.Handlers;

using Moq;
using SinalVortex.Application.Commands.Webhooks;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Dtos.Webhooks;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Models;
using SinalVortex.Domain.ValueObjects;
using Xunit;

public class ProcessarInboundWebhookCommandHandlerTests
{
    private readonly Mock<INotificacaoRepository> _repositoryMock = new();
    private readonly ProcessarInboundWebhookCommandHandler _handler;

    public ProcessarInboundWebhookCommandHandlerTests()
    {
        _handler = new ProcessarInboundWebhookCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_DeveAtualizarStatusParaEnviado_QuandoEventoForEntregue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var aplicacaoId = Guid.NewGuid();
        var destinatario = Destinatario.Criar("test@example.com", CanalNotificacao.Email); 

        // Construtor: (tenantId, aplicacaoId, destinatario, canal, prioridade, conteudo, contatoId, assunto)
        var notificacao = new Notificacao(
            tenantId,
            aplicacaoId, 
            destinatario, 
            CanalNotificacao.Email, 
            PrioridadeNotificacao.Normal,
            "Conteúdo da notificação",
            null,
            "Assunto da notificação"
        );
        
        var notificacaoId = notificacao.Id;
        notificacao.IniciarProcessamento();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(notificacaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notificacao);

        var payload = new InboundWebhookPayloadDto("SendGrid", notificacaoId, EventoWebhook.Entregue, null, DateTime.UtcNow);
        var command = new ProcessarInboundWebhookCommand(payload);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusNotificacao.Enviado, notificacao.Status);
        _repositoryMock.Verify(r => r.AtualizarAsync(notificacao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalse_QuandoNotificacaoNaoExistir()
    {
        // Arrange
        var notificacaoId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(notificacaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notificacao?)null);

        var payload = new InboundWebhookPayloadDto("SendGrid", notificacaoId, EventoWebhook.Entregue, null, DateTime.UtcNow);
        var command = new ProcessarInboundWebhookCommand(payload);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Notificacao>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}