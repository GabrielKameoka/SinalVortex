using Microsoft.AspNetCore.Hosting;

namespace SinalVortex.IntegrationTests;

public class CorsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData("https://sinal-vortex.vercel.app", true)]
    [InlineData("http://localhost:4200", false)]
    [InlineData("https://untrusted.example", false)]
    public async Task ProductionPreflight_OnlyAllowsFrontend(string origin, bool allowed)
    {
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/autenticacao/token");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
            Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        }
    }
}
