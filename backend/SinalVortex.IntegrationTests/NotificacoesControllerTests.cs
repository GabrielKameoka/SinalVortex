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
    
    [Fact]
    public async Task GetNotificacaoPorId_DeveRetornarOk_QuandoNotificacaoExistir()
    {
        // Arrange: Cria uma notificação primeiro via POST
        var command = new
        {
            aplicacaoId = Guid.NewGuid(),
            destinatario = "dev@sinalvortex.com",
            canal = 1,
            prioridade = 1,
            assunto = "Teste Consulta GET",
            conteudo = "Conteudo de teste para consulta."
        };

        var postResponse = await _client.PostAsJsonAsync("/api/v1/Notificacoes", command);
        var criada = await postResponse.Content.ReadFromJsonAsync<NotificacaoResponse>();

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/Notificacoes/{criada!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task ReprocessarNotificacao_DeveRetornarAcceptedOuOk()
    {
        // Arrange
        var command = new
        {
            aplicacaoId = Guid.NewGuid(),
            destinatario = "dev@sinalvortex.com",
            canal = 1,
            prioridade = 1,
            assunto = "Teste Reprocessar",
            conteudo = "Conteudo para reprocessamento."
        };

        var postResponse = await _client.PostAsJsonAsync("/api/v1/Notificacoes", command);
        var criada = await postResponse.Content.ReadFromJsonAsync<NotificacaoResponse>();

        // Act
        var reprocessarResponse = await _client.PostAsync($"/api/v1/Notificacoes/{criada!.Id}/reprocessar", null);

        // Assert
        Assert.True(reprocessarResponse.StatusCode == HttpStatusCode.OK || 
                    reprocessarResponse.StatusCode == HttpStatusCode.Accepted);
    }

    private record NotificacaoResponse(Guid Id, int Status, DateTime CriadoEm);
}