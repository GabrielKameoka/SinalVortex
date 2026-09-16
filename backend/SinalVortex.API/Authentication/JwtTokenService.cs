using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Entities;

namespace SinalVortex.API.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public AuthTokenDto CriarToken(Usuario usuario)
    {
        var settings = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("tenant_id", usuario.TenantId.ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            notBefore: DateTime.UtcNow, expires: expiresAt, signingCredentials: credentials);

        return new(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
