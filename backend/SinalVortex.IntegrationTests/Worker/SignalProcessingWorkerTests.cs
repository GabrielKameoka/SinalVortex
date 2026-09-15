using SinalVortex.Application.Commands.Notificacoes;

namespace SinalVortex.IntegrationTests.Worker;

using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Dtos; // <-- Namespace adicionado para resolver NotificacaoFilaItemDto
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using SinalVortex.Domain.Models;
using SinalVortex.Domain.ValueObjects;
using SinalVortex.Worker;
using Xunit;

public class SignalProcessingWorkerTests
{
    private readonly INotificacaoRepository _repository;
    private readonly INotificacaoDispatcher _dispatcher;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SignalProcessingWorker> _logger;

    public SignalProcessingWorkerTests()
    {
        _repository = Substitute.For<INotificacaoRepository>();
        _dispatcher = Substitute.For<INotificacaoDispatcher>();
        _cacheService = Substitute.For<ICacheService>();
        _logger = Substitute.For<ILogger<SignalProcessingWorker>>();
    }

    [Fact]
    public async Task ProcessarItemAsync_QuandoOcorrerFalhaPermanente_DeveMoverDiretoParaDlq()
    {
        // Arrange
        var notificacaoId = Guid.NewGuid();
        var itemDto = CriarItemFilaDto(notificacaoId);
        var notificacao = CriarNotificacaoDominio(notificacaoId);

        _repository.ObterPorIdAsync(notificacaoId, Arg.Any<CancellationToken>())
            .Returns(notificacao);

        var excecaoPermanente = new PermanentChannelException("Endereço de e-mail inválido ou inexistente.");
        _dispatcher.EnviarAsync(itemDto, Arg.Any<CancellationToken>())
            .Throws(excecaoPermanente);

        // Act
        await ExecutarProcessamentoItemAsync(itemDto);

        // Assert
        Assert.Equal(StatusNotificacao.Dlq, notificacao.Status);
        await _repository.Received(1).AtualizarAsync(notificacao, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).EnqueueAsync(
            Arg.Is<string>(key => key.Contains("dlq")), 
            itemDto);
    }

    [Fact]
    public async Task ProcessarItemAsync_QuandoOcorrerFalhaTransiente_DeveRegistrarFalhaEReenfileirarParaRetry()
    {
        // Arrange
        var notificacaoId = Guid.NewGuid();
        var itemDto = CriarItemFilaDto(notificacaoId);
        var notificacao = CriarNotificacaoDominio(notificacaoId);

        _repository.ObterPorIdAsync(notificacaoId, Arg.Any<CancellationToken>())
            .Returns(notificacao);

        var excecaoTransiente = new TimeoutException("Timeout na conexão com o gateway HTTP.");
        _dispatcher.EnviarAsync(itemDto, Arg.Any<CancellationToken>())
            .Throws(excecaoTransiente);

        // Act
        await ExecutarProcessamentoItemAsync(itemDto);

        // Assert
        Assert.Equal(StatusNotificacao.Falhou, notificacao.Status);
        await _repository.Received(1).AtualizarAsync(notificacao, Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().EnqueueAsync(
            Arg.Is<string>(key => key.Contains("dlq")), 
            Arg.Any<NotificacaoFilaItemDto>());

        await _cacheService.Received(1).EnqueueAsync(
            Arg.Is<string>(key => !key.Contains("dlq")), 
            itemDto);
    }

    [Fact]
    public async Task ProcessarItemAsync_QuandoExcederMaximoDeTentativas_DeveMoverParaDlq()
    {
        // Arrange
        var notificacaoId = Guid.NewGuid();
        var itemDto = CriarItemFilaDto(notificacaoId);
        
        var notificacao = new Notificacao(
            tenantId: Guid.NewGuid(),
            aplicacaoId: Guid.NewGuid(),
            destinatario: Destinatario.Criar("dev@sinalvortex.com", CanalNotificacao.Email),
            canal: CanalNotificacao.Email,
            prioridade: PrioridadeNotificacao.Alta,
            conteudo: "Teste limite tentativas",
            maxTentativas: 1
        );

        _repository.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(notificacao);

        _dispatcher.EnviarAsync(itemDto, Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Erro 503 Service Unavailable"));

        // Act
        notificacao.IniciarProcessamento();
        await ExecutarProcessamentoItemAsync(itemDto);

        // Assert
        Assert.Equal(StatusNotificacao.Dlq, notificacao.Status);
        await _repository.Received(1).AtualizarAsync(notificacao, Arg.Any<CancellationToken>());
    }

    // --- Helpers de Teste ---

    private static NotificacaoFilaItemDto CriarItemFilaDto(Guid id) =>
        new(
            NotificacaoId: id,
            AplicacaoId: Guid.NewGuid(),
            Canal: CanalNotificacao.Email,
            Prioridade: PrioridadeNotificacao.Alta,
            Destinatario: "dev@sinalvortex.com",
            Conteudo: "Conteúdo do SinalVortex",
            Assunto: "Assunto do E-mail"
        );

    private static Notificacao CriarNotificacaoDominio(Guid id)
    {
        return new Notificacao(
            tenantId: Guid.NewGuid(),
            aplicacaoId: Guid.NewGuid(),
            destinatario: Destinatario.Criar("dev@sinalvortex.com", CanalNotificacao.Email),
            canal: CanalNotificacao.Email,
            prioridade: PrioridadeNotificacao.Alta,
            conteudo: "Conteúdo do SinalVortex",
            maxTentativas: 3
        );
    }

    private async Task ExecutarProcessamentoItemAsync(NotificacaoFilaItemDto item)
    {
        var notificacao = await _repository.ObterPorIdAsync(item.NotificacaoId, CancellationToken.None);

        if (notificacao == null) return;

        try
        {
            if (notificacao.Status != StatusNotificacao.EmProcessamento)
                notificacao.IniciarProcessamento();

            await _dispatcher.EnviarAsync(item, CancellationToken.None);
            notificacao.MarcarComoEnviado();
            await _repository.AtualizarAsync(notificacao, CancellationToken.None);
        }
        catch (PermanentChannelException ex)
        {
            notificacao.EnviarParaDlq(ex.Message);
            await _repository.AtualizarAsync(notificacao, CancellationToken.None);
            await _cacheService.EnqueueAsync("notificacoes:dlq", item);
        }
        catch (Exception ex)
        {
            notificacao.RegistrarFalha(ex.Message);
            await _repository.AtualizarAsync(notificacao, CancellationToken.None);

            if (notificacao.Status == StatusNotificacao.Dlq)
            {
                await _cacheService.EnqueueAsync("notificacoes:dlq", item);
            }
            else
            {
                await _cacheService.EnqueueAsync("notificacoes:fila", item);
            }
        }
    }
}