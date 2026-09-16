using MediatR;
using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Commands.Contatos;

namespace SinalVortex.API.Controllers;

[ApiController]
[Route("api/v1/contatos")]
public sealed class ContatosController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarContatoCRMCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpGet]
    public Task<ContatoPageDto> Listar([FromQuery] ObterContatosQuery query, CancellationToken cancellationToken) => mediator.Send(query, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObterContatoQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarContatoBody body, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new AtualizarContatoCommand(id, body.Nome, body.Email, body.Telefone, body.CanalPreferencial), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new RemoverContatoCommand(id), cancellationToken) ? NoContent() : NotFound();

    public sealed record AtualizarContatoBody(string Nome, string? Email, string? Telefone, SinalVortex.Domain.Enums.CanalNotificacao CanalPreferencial);
}
