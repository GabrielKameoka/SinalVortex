using System.Net;
using System.Net.Http.Json;
using SinalVortex.Application.Commands.Autenticacao;

namespace SinalVortex.IntegrationTests;

public class RegistrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Register_CanLoginAgainWithReturnedTenant()
    {
        using var client = factory.CreateClient();
        var email = $"registration-{Guid.NewGuid():N}@example.test";
        const string password = "registration-test-password-123";
        using var registration = await client.PostAsJsonAsync("/api/v1/autenticacao/registrar",
            new { nome = "Registration Test", email, senha = password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var registered = await registration.Content.ReadFromJsonAsync<AutenticacaoDto>();
        Assert.NotNull(registered);
        Assert.NotEqual(Guid.Empty, registered.TenantId);
        Assert.False(string.IsNullOrWhiteSpace(registered.AccessToken));

        using var login = await client.PostAsJsonAsync("/api/v1/autenticacao/token",
            new { tenantId = registered.TenantId, email, senha = password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var authenticated = await login.Content.ReadFromJsonAsync<AutenticacaoDto>();
        Assert.Equal(registered.UsuarioId, authenticated!.UsuarioId);
        Assert.Equal(registered.TenantId, authenticated.TenantId);

        using var wrongTenant = await client.PostAsJsonAsync("/api/v1/autenticacao/token",
            new { tenantId = Guid.NewGuid(), email, senha = password });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongTenant.StatusCode);
    }

    [Fact]
    public async Task Register_RejectsShortPassword()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/autenticacao/registrar",
            new { nome = "Registration Test", email = "short@example.test", senha = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

