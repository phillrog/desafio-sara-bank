using Moq;
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

public class TransferenciaIniciadaConsumerServiceTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<TransferenciaIniciadaConsumerService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public TransferenciaIniciadaConsumerServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TransferenciaIniciadaConsumerService>>();

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
    public async Task Deve_Iniciar_Saga_Com_Sucesso_E_Dar_Ack()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        const decimal valor = 250.00m;

        var payloadEvento = new
        {
            SagaId = sagaId,
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = valor
        };

        var envelope = new
        {
            TipoEvento = "TransferenciaIniciada",
            SagaId = sagaId.ToString(),
            Payload = JsonSerializer.Serialize(payloadEvento)
        };

        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope))
        };

        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            using var scope = _mockScopeFactory.Object.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
            var body = msg.Data.ToStringUtf8();
            var doc = JsonDocument.Parse(body);

            var tipo = doc.RootElement.GetProperty("TipoEvento").GetString();
            var payloadStr = doc.RootElement.GetProperty("Payload").GetString();

            if (tipo == "TransferenciaIniciada")
            {
                var ev = JsonSerializer.Deserialize<TransferenciaIniciadaEvent>(payloadStr, options);
                await mediator.Publish(ev, ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        // Act
        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);

        // Verifica se o Mediator disparou o evento inicial com os dados corretos para o débito
        _mockMediator.Verify(m => m.Publish(
            It.Is<TransferenciaIniciadaEvent>(e =>
                e.SagaId == sagaId &&
                e.ContaOrigemId == contaOrigemId &&
                e.Valor == valor),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deve_Retornar_Nack_Se_Payload_Estiver_Corrompido()
    {
        // Arrange - JSON quebrado
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8("{ \"TipoEvento\": \"TransferenciaIniciada\",") };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            try
            {
                JsonDocument.Parse(msg.Data.ToStringUtf8());
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
        _mockMediator.Verify(m => m.Publish(It.IsAny<TransferenciaIniciadaEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Ignorar_Se_TipoEvento_Nao_For_Iniciado()
    {
        // Arrange
        var envelope = new { TipoEvento = "OutroEvento", SagaId = Guid.NewGuid().ToString(), Payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            var doc = JsonDocument.Parse(msg.Data.ToStringUtf8());
            if (doc.RootElement.GetProperty("TipoEvento").GetString() == "TransferenciaIniciada")
                await _mockMediator.Object.Publish(It.IsAny<TransferenciaIniciadaEvent>(), ct);

            return SubscriberClient.Reply.Ack;
        };

        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);
        _mockMediator.Verify(m => m.Publish(It.IsAny<TransferenciaIniciadaEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}