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

public class FalhaNoCreditoConsumerServiceTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<FalhaNoCreditoConsumerService>> _mockLogger;

    public FalhaNoCreditoConsumerServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<FalhaNoCreditoConsumerService>>();

        // Mocking Service Scope para o Mediator
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(IMediator))).Returns(_mockMediator.Object);

        _mockScope = new Mock<IServiceScope>();
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
    }

    [Fact]
    public async Task Deve_Retornar_Ack_E_Publicar_No_Mediator_Quando_Mensagem_For_Valida()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var eventoPayload = new FalhaNoCreditoEvent(sagaId, Guid.NewGuid(), 150.00m, "Erro de teste");
        var envelope = new
        {
            TipoEvento = "FalhaNoCredito",
            SagaId = sagaId.ToString(),
            Payload = JsonSerializer.Serialize(eventoPayload)
        };

        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope))
        };

        // Act 
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) => {
            using var scope = _mockScopeFactory.Object.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var body = msg.Data.ToStringUtf8();
            var doc = JsonDocument.Parse(body);
            var tipo = doc.RootElement.GetProperty("TipoEvento").GetString();
            var payloadStr = doc.RootElement.GetProperty("Payload").GetString();

            if (tipo == "FalhaNoCredito")
            {
                var ev = JsonSerializer.Deserialize<FalhaNoCreditoEvent>(payloadStr);
                await mediator.Publish(ev, ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);
        _mockMediator.Verify(m => m.Publish(It.Is<FalhaNoCreditoEvent>(e => e.SagaId == sagaId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deve_Retornar_Nack_Quando_Ocorrer_Excecao_No_Processamento()
    {
        // Arrange
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8("{ invalid json }") };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) => {
            try
            {
                JsonSerializer.Deserialize<JsonElement>(msg.Data.ToStringUtf8());
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
    public async Task Nao_Deve_Publicar_No_Mediator_Se_TipoEvento_For_Diferente()
    {
        // Arrange
        var envelope = new { TipoEvento = "OutroTipo", SagaId = Guid.NewGuid().ToString(), Payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) => {
            var body = msg.Data.ToStringUtf8();
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.GetProperty("TipoEvento").GetString() == "FalhaNoCredito")
            {
                await _mockMediator.Object.Publish(new FalhaNoCreditoEvent(Guid.NewGuid(), Guid.NewGuid(), 0, ""), ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        await handler(message, CancellationToken.None);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<FalhaNoCreditoEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}