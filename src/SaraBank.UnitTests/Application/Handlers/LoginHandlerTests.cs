using Moq;
using Xunit;
using FluentAssertions;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Application.Commands;
using SaraBank.Domain.Interfaces;

namespace SaraBank.UnitTests.Application.Handlers;

public class LoginHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _handler = new LoginHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarToken_QuandoCredenciaisForemValidas()
    {
        // Arrange
        var command = new LoginCommand("usuario@teste.com", "Senha@123");
        var tokenEsperado = "jwt-token-gerado-pelo-firebase";

        _identityServiceMock
            .Setup(x => x.AutenticarAsync(command.Email, command.Senha))
            .ReturnsAsync(tokenEsperado);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Should().Be(tokenEsperado);


        _identityServiceMock.Verify(x => x.AutenticarAsync(command.Email, command.Senha), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRepassarExcecao_QuandoServicoFalhar()
    {
        // Arrange
        var command = new LoginCommand("invalido@teste.com", "senha-errada");
        var mensagemErro = "E-mail ou senha inválidos.";

        _identityServiceMock
            .Setup(x => x.AutenticarAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception(mensagemErro));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage(mensagemErro);
    }
}