using SinalVortex.Application.Commands.Contatos;

namespace SinalVortex.UnitTests.Application.CRM;

using NSubstitute;
using Xunit;

// 1. O namespace onde está a interface do repositório (ex: IContatoRepository)
using SinalVortex.Application.Common.Interfaces; 

// 2. O namespace real onde está o seu Handler e o Command (ex: Contatos ou Notificacoes)
using SinalVortex.Application.Commands.Contatos; // <--- Substitua pela pasta/namespace real do comando

// 3. Namespaces do Domínio
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Enums;

public class CriarContatoCRMCommandHandlerTests
{
    private readonly IContatoRepository _contatoRepositoryMock;
    private readonly ITenantContext _tenantContextMock;
    private readonly CriarContatoCRMCommandHandler _handler;

    public CriarContatoCRMCommandHandlerTests()
    {
        _contatoRepositoryMock = Substitute.For<IContatoRepository>();
        _tenantContextMock = Substitute.For<ITenantContext>();

        _handler = new CriarContatoCRMCommandHandler(
            _contatoRepositoryMock,
            _tenantContextMock
        );
    }

    [Fact]
    public async Task Handle_QuandoComandoForValido_DeveAtribuirTenantIdDoContextoAoContato()
    {
        // Arrange
        var tenantIdEsperado = Guid.NewGuid();
        _tenantContextMock.TenantId.Returns(tenantIdEsperado);

        var command = new CriarContatoCRMCommand(
            Nome: "Gabriel Namazu",
            Email: "gabriel@vortex.com",
            Telefone: "11999999999",
            CanalPreferencial: CanalNotificacao.Email
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Valida se o repositório recebeu a entidade Contato exatamente com o TenantId do contexto
        await _contatoRepositoryMock.Received(1).AdicionarAsync(
            Arg.Is<Contato>(c => c.TenantId == tenantIdEsperado && c.Nome == "Gabriel Namazu"),
            Arg.Any<CancellationToken>()
        );
    }
}