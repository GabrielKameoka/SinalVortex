using Microsoft.AspNetCore.Identity;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Entities;

namespace SinalVortex.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
