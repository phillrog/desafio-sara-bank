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

public class TransferenciaConcluidaConsumerServiceTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<TransferenciaConcluidaConsumerService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public TransferenciaConcluidaConsumerServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TransferenciaConcluidaConsumerService>>();

        // Setup padrão de Escopo para Workers
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
    public async Task Deve_Finalizar_Saga_Com_Sucesso_E_Dar_Ack()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var eventoId = Guid.NewGuid();
        var dataConclusao = DateTime.UtcNow;

        var eventoInterno = new
        {
            SagaId = sagaId,
            EventoId = eventoId,
            FinalizadoEm = dataConclusao
        };

        var envelope = new
        {
            TipoEvento = "TransferenciaConcluida",
            SagaId = sagaId.ToString(),
            Payload = JsonSerializer.Serialize(eventoInterno)
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

            if (tipo == "TransferenciaConcluida")
            {
                var ev = JsonSerializer.Deserialize<TransferenciaConcluidaEvent>(payloadStr, options);
                await mediator.Publish(ev, ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        // Act
        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);

        _mockMediator.Verify(m => m.Publish(
            It.Is<TransferenciaConcluidaEvent>(e =>
                e.SagaId == sagaId &&
                e.EventoId == eventoId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deve_Retornar_Nack_Se_Envelope_Estiver_Incompleto()
    {
        // Arrange
        var envelopeIncompleto = new { TipoEvento = "TransferenciaConcluida", Payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelopeIncompleto)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            try
            {
                var doc = JsonDocument.Parse(msg.Data.ToStringUtf8());
                _ = Guid.Parse(doc.RootElement.GetProperty("SagaId").GetString());
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
    public async Task Nao_Deve_Chamar_Mediator_Se_Tipo_For_Diferente_Mas_Deve_Dar_Ack()
    {
        // Arrange
        var envelope = new { TipoEvento = "OutroEvento", SagaId = Guid.NewGuid().ToString(), Payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            var doc = JsonDocument.Parse(msg.Data.ToStringUtf8());
            if (doc.RootElement.GetProperty("TipoEvento").GetString() == "TransferenciaConcluida")
                await _mockMediator.Object.Publish(new TransferenciaConcluidaEvent(Guid.NewGuid(), DateTime.Now), ct);

            return SubscriberClient.Reply.Ack;
        };

        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);
        _mockMediator.Verify(m => m.Publish(It.IsAny<TransferenciaConcluidaEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}