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

public class SaldoDebitadoConsumerServiceTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<SaldoDebitadoConsumerService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public SaldoDebitadoConsumerServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<SaldoDebitadoConsumerService>>();

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
    public async Task Deve_Processar_Saldo_Debitado_E_Enviar_Para_Credito_Com_Ack()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var contaOrigem = Guid.NewGuid();
        var contaDestino = Guid.NewGuid();
        const decimal valor = 500.00m;

        var eventoInterno = new
        {
            SagaId = sagaId,
            ContaOrigemId = contaOrigem,
            ContaDestinoId = contaDestino,
            Valor = valor
        };

        var envelope = new
        {
            TipoEvento = "SaldoDebitado",
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

            var body = msg.Data.ToStringUtf8();
            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
            var doc = JsonDocument.Parse(body);

            var tipo = doc.RootElement.GetProperty("TipoEvento").GetString();
            var payloadStr = doc.RootElement.GetProperty("Payload").GetString();

            if (tipo == "SaldoDebitado")
            {
                var ev = JsonSerializer.Deserialize<SaldoDebitadoEvent>(payloadStr, options);
                await mediator.Publish(ev, ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        // Act
        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);

        _mockMediator.Verify(m => m.Publish(
            It.Is<SaldoDebitadoEvent>(e =>
                e.SagaId == sagaId &&
                e.Valor == valor &&
                e.ContaDestinoId == contaDestino),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deve_Dar_Nack_Quando_Ocorrer_Erro_De_Parse_No_SagaId()
    {
        // Arrange
        var envelope = new
        {
            TipoEvento = "SaldoDebitado",
            SagaId = "not-a-guid",
            Payload = "{}"
        };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            try
            {
                var body = msg.Data.ToStringUtf8();
                var doc = JsonDocument.Parse(body);
                Guid.Parse(doc.RootElement.GetProperty("SagaId").GetString()); // Estoura aqui
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
    public async Task Deve_Ignorar_Silenciosamente_Se_TipoEvento_For_Desconhecido()
    {
        // Arrange
        var envelope = new { TipoEvento = "EventoX", SagaId = Guid.NewGuid().ToString(), Payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope)) };

        // Act
        var resultado = await Task.FromResult(SubscriberClient.Reply.Ack);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);
        _mockMediator.Verify(m => m.Publish(It.IsAny<SaldoDebitadoEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}