using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Commands.Autenticacao;

namespace SinalVortex.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/autenticacao")]
public sealed class AutenticacaoController(ISender mediator) : ControllerBase
{
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(AutenticacaoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarTenantCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("token")]
    [ProducesResponseType(typeof(AutenticacaoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Token([FromBody] AutenticarCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await mediator.Send(command, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Mensagem = "Credenciais inválidas." });
        }
    }
}
