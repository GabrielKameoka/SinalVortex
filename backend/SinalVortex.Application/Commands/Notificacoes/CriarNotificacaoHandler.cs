using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Models;
using SinalVortex.Domain.ValueObjects;

namespace SinalVortex.Application.Commands.Notificacoes;

public class CriarNotificacaoCommandHandler : IRequestHandler<CriarNotificacaoCommand, CriarNotificacaoResultDto>
{
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly ICacheService _cacheService;

    public CriarNotificacaoCommandHandler(INotificacaoRepository notificacaoRepository, ICacheService cacheService)
    {
        _notificacaoRepository = notificacaoRepository;
        _cacheService = cacheService;
    }

    public async Task<CriarNotificacaoResultDto> Handle(CriarNotificacaoCommand request, CancellationToken cancellationToken)
    {
        // 1. Instancia e valida o Value Object de domínio conforme o canal
        var destinatario = Destinatario.Criar(request.Destinatario, request.Canal);

        // 2. Cria o Agregado Notificacao (em estado inicial Pendente)
        var notificacao = new Notificacao(
            request.AplicacaoId,
            destinatario,
            request.Canal,
            request.Prioridade,
            request.Conteudo,
            request.Assunto,
            request.TemplateId
        );

        // 3. Persiste via repositório de domínio
        await _notificacaoRepository.AdicionarAsync(notificacao, cancellationToken);

        // 4. Roteia para a fila correspondente à prioridade no Redis
        string filaKey = $"notificacoes:fila:{request.Prioridade.ToString().ToLower()}";

        var payloadFila = new NotificacaoFilaItemDto(
            notificacao.Id,
            notificacao.AplicacaoId,
            notificacao.Canal,
            notificacao.Prioridade,
            notificacao.Destinatario.Valor,
            notificacao.Conteudo,
            notificacao.Assunto
        );

        await _cacheService.EnqueueAsync(filaKey, payloadFila);

        // 5. Retorna o resultado para a camada de apresentação
        return new CriarNotificacaoResultDto(
            notificacao.Id,
            notificacao.Status,
            notificacao.CriadoEm
        );
    }
}
//Executam os casos de uso da aplicação. O Command carrega apenas os dados da intenção (ex: CriarNotificacaoCommand), enquanto o CommandHandler executa as regras de negócio associadas (validar, salvar no banco, enfileirar no Redis).