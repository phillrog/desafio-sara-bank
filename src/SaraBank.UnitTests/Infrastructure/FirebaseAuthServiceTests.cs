using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Configuration;
using SaraBank.Infrastructure.Services;
using System.Net;
using System.Text.Json;

namespace SaraBank.UnitTests.Infrastructure;

public class FirebaseAuthServiceTests
{
    private readonly Mock<IFirebaseAuthWrapper> _authWrapperMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly FirebaseAuthService _service;

    public FirebaseAuthServiceTests()
    {
        _authWrapperMock = new Mock<IFirebaseAuthWrapper>();
        _configMock = new Mock<IConfiguration>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        // Simula a API Key do Firebase vinda do appsettings.json
        _configMock.Setup(c => c["Firebase:ApiKey"]).Returns("test-api-key");

        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _service = new FirebaseAuthService(_authWrapperMock.Object, _httpClient, _configMock.Object);
    }

    [Fact]
    public async Task CriarUsuarioAsync_DeveChamarWrapperComDadosCorretos()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = "teste@sarabank.com";
        var senha = "Password123";
        var nome = "Teste Silva";

        _authWrapperMock.Setup(x => x.CreateUserAsync(It.IsAny<UserRecordArgs>()))
                        .Returns(Task.CompletedTask);

        // Act
        await _service.CriarUsuarioAsync(id, email, senha, nome);

        // Assert
        _authWrapperMock.Verify(x => x.CreateUserAsync(It.Is<UserRecordArgs>(args =>
            args.Uid == id.ToString() &&
            args.Email == email &&
            args.Password == senha &&
            args.DisplayName == nome)), Times.Once);
    }

    [Fact]
    public async Task AutenticarAsync_DeveRetornarIdToken_QuandoSucesso()
    {
        // Arrange
        var email = "teste@sarabank.com";
        var senha = "Password123";
        var expectedToken = "fake-jwt-token";

        var responseContent = new { idToken = expectedToken, email = email, localId = "123" };
        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        // Act
        var result = await _service.AutenticarAsync(email, senha);

        // Assert
        result.Should().Be(expectedToken);
    }

    [Fact]
    public async Task AutenticarAsync_DeveLancarExcecao_QuandoCredenciaisInvalidas()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.BadRequest, new { error = "INVALID_PASSWORD" });

        // Act
        Func<Task> act = async () => await _service.AutenticarAsync("erro@t.com", "123");

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("E-mail ou senha inválidos.");
    }

    [Fact]
    public async Task DeletarUsuarioAsync_DeveChamarWrapperComIdCorreto()
    {
        // Arrange
        var id = Guid.NewGuid();
        _authWrapperMock.Setup(x => x.DeleteUserAsync(id.ToString())).Returns(Task.CompletedTask);

        // Act
        await _service.DeletarUsuarioAsync(id);

        // Assert
        _authWrapperMock.Verify(x => x.DeleteUserAsync(id.ToString()), Times.Once);
    }

    // Método auxiliar para simular respostas HTTP
    private void SetupHttpResponse(HttpStatusCode code, object content)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = code,
                Content = new StringContent(JsonSerializer.Serialize(content))
            });
    }
}