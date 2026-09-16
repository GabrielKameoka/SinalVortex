using MediatR;
using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Commands.Templates;

namespace SinalVortex.API.Controllers;

[ApiController]
[Route("api/v1/templates")]
public sealed class TemplatesController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarTemplateCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    [HttpGet]
    public Task<TemplatePageDto> Listar([FromQuery] ObterTemplatesQuery query, CancellationToken cancellationToken) => mediator.Send(query, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObterTemplateQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarTemplateBody body, CancellationToken cancellationToken) =>
        await mediator.Send(new AtualizarTemplateCommand(id, body.Conteudo), cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new RemoverTemplateCommand(id), cancellationToken) ? NoContent() : NotFound();

    public sealed record AtualizarTemplateBody(string Conteudo);
}
