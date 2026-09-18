using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Models;

namespace SinalVortex.API.Controllers;

[ApiController]
[Route("api/v1/aplicacoes")]
[Produces("application/json")]
public sealed class AplicacoesController(
    IAplicacaoRepository repository,
    ITenantContext tenantContext,
    IPasswordHasher passwordHasher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AplicacaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var aplicacoes = await repository.ListarAsync(cancellationToken);
        return Ok(aplicacoes.Select(ToDto).ToArray());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AplicacaoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var aplicacao = await repository.ObterPorIdAsync(id, cancellationToken);
        return aplicacao is null ? NotFound() : Ok(ToDto(aplicacao));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CriarAplicacaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAplicacaoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { Mensagem = "O nome da aplicação é obrigatório." });

        var apiKey = $"sv_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
        var aplicacao = new Aplicacoes(tenantContext.TenantId, request.Nome, passwordHasher.Hash(apiKey));
        await repository.AdicionarAsync(aplicacao, cancellationToken);

        return CreatedAtAction(nameof(Obter), new { id = aplicacao.Id }, new CriarAplicacaoResponse(
            aplicacao.Id, aplicacao.Nome, apiKey, aplicacao.Ativo, aplicacao.CriadoEm));
    }

    private static AplicacaoDto ToDto(Aplicacoes aplicacao) => new(aplicacao.Id, aplicacao.Nome, aplicacao.Ativo, aplicacao.CriadoEm);
}

public sealed record CriarAplicacaoRequest(string Nome);
public sealed record AplicacaoDto(Guid Id, string Nome, bool Ativo, DateTime CriadaEm);
public sealed record CriarAplicacaoResponse(Guid Id, string Nome, string ApiKey, bool Ativo, DateTime CriadaEm);
