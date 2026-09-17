using SinalVortex.Domain.Entities;

namespace SinalVortex.Application.Common.Interfaces;

public interface ITokenService
{
    AuthTokenDto CriarToken(Usuario usuario);
}

public sealed record AuthTokenDto(string AccessToken, DateTime ExpiresAtUtc);
