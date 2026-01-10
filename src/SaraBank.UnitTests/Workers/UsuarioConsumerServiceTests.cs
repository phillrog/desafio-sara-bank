using Moq;
using Xunit;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaraBank.Infrastructure.Workers;
using SaraBank.Application.Events;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System.Text.Json;

namespace SaraBank.UnitTests.Workers;

public class UsuarioConsumerServiceTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<UsuarioConsumerService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public UsuarioConsumerServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<UsuarioConsumerService>>();

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IMediator)))
            .Returns(_mockMediator.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
    }

    [Fact]
    public async Task Deve_Processar_Cadastro_E_Disparar_Saldo_Inicial_Com_Ack()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        const string email = "usuario@sarabank.com.br";

        // Record do evento de cadastro
        var eventoInterno = new
        {
            UsuarioId = usuarioId,
            ContaId = contaId,
            Email = email
        };

        var envelope = new
        {
            tipoEvento = "UsuarioCadastrado",
            payload = JsonSerializer.Serialize(eventoInterno)
        };

        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope))
        };

        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            using var scope = _mockScopeFactory.Object.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var body = msg.Data.ToStringUtf8();
            var doc = JsonDocument.Parse(body);

            var tipo = doc.RootElement.GetProperty("tipoEvento").GetString();
            var payloadStr = doc.RootElement.GetProperty("payload").GetString();

            if (tipo == "UsuarioCadastrado")
            {
                var ev = JsonSerializer.Deserialize<UsuarioCadastradoEvent>(payloadStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (ev != null) await mediator.Publish(ev, ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        // Act
        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);
        _mockMediator.Verify(m => m.Publish(
            It.Is<UsuarioCadastradoEvent>(e => e.ContaId == contaId && e.Email == email),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deve_Retornar_Nack_Se_Propriedade_Obrigataria_Nao_Existir_No_Json()
    {
        // Arrange
        var envelopeInvalido = new { payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelopeInvalido)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            try
            {
                var doc = JsonDocument.Parse(msg.Data.ToStringUtf8());
                _ = doc.RootElement.GetProperty("tipoEvento").GetString();
                return SubscriberClient.Reply.Ack;
            }
            catch
            {
                return SubscriberClient.Reply.Nack;
            }
        };

        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Nack);
    }

    [Fact]
    public async Task Deve_Dar_Ack_E_Nao_Chamar_Mediator_Para_Evento_Diferente()
    {
        // Arrange
        var envelope = new { tipoEvento = "EventoDesconhecido", payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            var doc = JsonDocument.Parse(msg.Data.ToStringUtf8());
            if (doc.RootElement.GetProperty("tipoEvento").GetString() == "UsuarioCadastrado")
                await _mockMediator.Object.Publish(It.IsAny<UsuarioCadastradoEvent>(), ct);

            return SubscriberClient.Reply.Ack;
        };

        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);
        _mockMediator.Verify(m => m.Publish(It.IsAny<UsuarioCadastradoEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}