using Moq;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaraBank.Infrastructure.Workers;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System.Text.Json;

namespace SaraBank.UnitTests.Workers;

public class MovimentacaoConsumerServiceTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<MovimentacaoConsumerService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public MovimentacaoConsumerServiceTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<MovimentacaoConsumerService>>();

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
    public async Task Deve_Processar_Movimentacao_Com_Sucesso_E_Dar_Ack()
    {
        var contaId = Guid.NewGuid();
        const decimal valor = 150.50m;
        const string tipoMov = "CREDITO";
        const string descricao = "Transferência Recebida";

        var eventoInterno = new
        {
            ContaId = contaId,
            Valor = valor,
            Tipo = tipoMov,
            Descricao = descricao
        };

        var envelope = new
        {
            TipoEvento = "NovaMovimentacao",
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
            var doc = JsonDocument.Parse(body);
            var tipoMsg = doc.RootElement.GetProperty("TipoEvento").GetString();
            var payloadStr = doc.RootElement.GetProperty("Payload").GetString();

            if (tipoMsg == "NovaMovimentacao")
            {
                var ev = JsonSerializer.Deserialize<NovaMovimentacaoEvent>(payloadStr);
                await mediator.Publish(ev, ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        // Act
        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);

        _mockMediator.Verify(m => m.Publish(
            It.Is<NovaMovimentacaoEvent>(e =>
                e.ContaId == contaId &&
                e.Valor == valor &&
                e.Tipo == tipoMov &&
                e.Descricao == descricao),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deve_Ignorar_Evento_E_Dar_Ack_Se_Tipo_For_Incorreto()
    {
        // Arrange
        var envelope = new { TipoEvento = "EventoDesconhecido", Payload = "{}" };
        var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(envelope)) };

        // Act
        Func<PubsubMessage, CancellationToken, Task<SubscriberClient.Reply>> handler = async (msg, ct) =>
        {
            var body = msg.Data.ToStringUtf8();
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.GetProperty("TipoEvento").GetString() == "NovaMovimentacao")
            {
                var ev = new NovaMovimentacaoEvent(Guid.NewGuid(), 0, "", "");
                await _mockMediator.Object.Publish(ev, ct);
            }
            return SubscriberClient.Reply.Ack;
        };

        var resultado = await handler(message, CancellationToken.None);

        // Assert
        resultado.Should().Be(SubscriberClient.Reply.Ack);
        _mockMediator.Verify(m => m.Publish(It.IsAny<NovaMovimentacaoEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}