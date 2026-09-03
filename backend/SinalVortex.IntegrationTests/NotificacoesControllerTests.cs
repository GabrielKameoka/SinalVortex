using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SinalVortex.IntegrationTests.Controllers;

public class NotificacoesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NotificacoesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostNotificacao_ComPayloadValido_DeveRetornarCreatedEId()
    {
        // Arrange
        var command = new
        {
            aplicacaoId = Guid.NewGuid(),
            destinatario = "dev@sinalvortex.com",
            canal = 1, // Email
            prioridade = 1,
            assunto = "Teste Testcontainers",
            conteudo = "Validando testes de integração sem infraestrutura local."
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/Notificacoes", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadFromJsonAsync<NotificacaoResponse>();
        Assert.NotNull(content);
        Assert.NotEqual(Guid.Empty, content.Id);
    }

    private record NotificacaoResponse(Guid Id, int Status, DateTime CriadoEm);
}