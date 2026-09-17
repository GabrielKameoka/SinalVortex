namespace SinalVortex.UnitTests.Domain.ValueObjects;

using SinalVortex.Domain.Enums;
using SinalVortex.Domain.Exceptions;
using SinalVortex.Domain.ValueObjects;
using Xunit;

public class DestinatarioTests
{
    [Theory]
    [InlineData("email-invalido")]
    [InlineData("usuario@")]
    [InlineData("sem-dominio.com")]
    public void Criar_QuandoEmailForInvalido_DeveLancarDomainException(string emailInvalido)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            Destinatario.Criar(emailInvalido, CanalNotificacao.Email));

        Assert.Contains("Endereço de e-mail inválido", ex.Message);
    }

    [Theory]
    [InlineData("(11) 99999-9999", "11999999999")]
    [InlineData("+55 (11) 98888-7777", "5511988887777")]
    public void Criar_QuandoTelefoneContiverFormatacao_DeveSanitizarParaApenasNumeros(string entrada, string esperado)
    {
        // Act
        var destinatario = Destinatario.Criar(entrada, CanalNotificacao.WhatsApp);

        // Assert
        Assert.Equal(esperado, destinatario.Valor);
    }
}