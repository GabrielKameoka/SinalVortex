using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Application.Commands.Autenticacao;

public sealed record RegistrarTenantCommand(string Nome, string Email, string Senha) : IRequest<AutenticacaoDto>;
public sealed record AutenticarCommand(Guid TenantId, string Email, string Senha) : IRequest<AutenticacaoDto>;
public sealed record AutenticacaoDto(Guid UsuarioId, Guid TenantId, string AccessToken, DateTime ExpiresAtUtc);

public sealed class RegistrarTenantCommandHandler(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<RegistrarTenantCommand, AutenticacaoDto>
{
    public async Task<AutenticacaoDto> Handle(RegistrarTenantCommand request, CancellationToken cancellationToken)
    {
        if (await usuarioRepository.EmailJaRegistradoAsync(request.Email, cancellationToken))
            throw new DomainException("Este e-mail já foi registrado. Entre na sua conta ou use outro e-mail.");

        var usuario = new Usuario(Guid.NewGuid(), request.Nome, request.Email, passwordHasher.Hash(request.Senha));
        await usuarioRepository.AdicionarAsync(usuario, cancellationToken);
        var token = tokenService.CriarToken(usuario);
        return new(usuario.Id, usuario.TenantId, token.AccessToken, token.ExpiresAtUtc);
    }
}

public sealed class AutenticarCommandHandler(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<AutenticarCommand, AutenticacaoDto>
{
    public async Task<AutenticacaoDto> Handle(AutenticarCommand request, CancellationToken cancellationToken)
    {
        var usuario = await usuarioRepository.ObterParaAutenticacaoAsync(request.Email, request.TenantId, cancellationToken);
        if (usuario is null || !usuario.Ativo || !passwordHasher.Verify(usuario.PasswordHash, request.Senha))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        var token = tokenService.CriarToken(usuario);
        return new(usuario.Id, usuario.TenantId, token.AccessToken, token.ExpiresAtUtc);
    }
}
