using MediatR;
using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Commands.Notificacoes;
using SinalVortex.Application.Queries.Notificacoes;

namespace SinalVortex.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class NotificacoesController : ControllerBase
{
    private readonly ISender _mediator;

    public NotificacoesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registra e enfileira uma nova solicitação de notificação.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CriarNotificacaoResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarNotificacaoCommand command,
        CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = resultado.Id },
            resultado);
    }

    /// <summary>
    /// Consulta o status de uma notificação por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificacaoDetalhesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new ObterNotificacaoPorIdQuery(id);
        var resultado = await _mediator.Send(query, cancellationToken);

        if (resultado is null)
            return NotFound(new { Mensagem = "Notificação não encontrada." });

        return Ok(resultado);
    }
    
    /// <summary>
    /// Consulta histórico de notificações com suporte a paginação e filtros.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedListDto<NotificacaoDetalhesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPaginado(
        [FromQuery] ObterNotificacoesPaginadasQuery query,
        CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(query, cancellationToken);
        return Ok(resultado);
    }
}