using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaraBank.Application.Events;
using SaraBank.Application.Handlers.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using Xunit;

namespace SaraBank.UnitTests.Handlers;

public class ProcessarCreditoSagaHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IContaRepository> _contaRepoMock;
    private readonly Mock<IMovimentacaoRepository> _movRepoMock;
    private readonly Mock<IOutboxRepository> _outboxRepoMock;
    private readonly Mock<ILogger<ProcessarCreditoSagaHandler>> _loggerMock;
    private readonly ProcessarCreditoSagaHandler _handler;

    public ProcessarCreditoSagaHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _contaRepoMock = new Mock<IContaRepository>();
        _movRepoMock = new Mock<IMovimentacaoRepository>();
        _outboxRepoMock = new Mock<IOutboxRepository>();
        _loggerMock = new Mock<ILogger<ProcessarCreditoSagaHandler>>();

        _uowMock.Setup(x => x.ExecutarAsync<bool>(It.IsAny<Func<Task<bool>>>()))
            .Returns<Func<Task<bool>>>(async (func) => await func());

        _handler = new ProcessarCreditoSagaHandler(
            _uowMock.Object,
            _contaRepoMock.Object,
            _movRepoMock.Object,
            _loggerMock.Object,
            _outboxRepoMock.Object);
    }

    [Fact]
    public async Task Deve_Gerar_Evento_Compensacao_No_Outbox_Quando_Conta_Destino_Nao_Existir()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var eventoDebito = new SaldoDebitadoEvent(
            SagaId: sagaId,
            ContaOrigemId: Guid.NewGuid(),
            ContaDestinoId: Guid.NewGuid(),
            Valor: 100.00m
        );

        _contaRepoMock.Setup(r => r.ObterPorIdAsync(eventoDebito.ContaDestinoId))
                      .ReturnsAsync((ContaCorrente?)null);

        OutboxMessage? mensagemReal = null;
        _outboxRepoMock.Setup(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
                       .Callback<OutboxMessage, CancellationToken>((m, ct) => mensagemReal = m);

        // Act
        await _handler.Handle(eventoDebito, CancellationToken.None);

        // Assert
        mensagemReal.Should().NotBeNull("O Handler deveria ter gerado a mensagem de compensação.");
        mensagemReal!.Tipo.Should().Be("FalhaNoCredito");
        mensagemReal!.Topico.Should().Be("sara-bank-transferencias-compensar");
    }

    [Fact]
    public async Task Handle_Deve_Ignorar_Processamento_Se_Credito_Ja_Foi_Realizado()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var evento = new SaldoDebitadoEvent(sagaId, Guid.NewGuid(), Guid.NewGuid(), 100m);

        _movRepoMock.Setup(m => m.ExisteMovimentacaoParaSagaAsync(sagaId, "CREDITO"))
                    .ReturnsAsync(true);

        // Act
        await _handler.Handle(evento, CancellationToken.None);

        // Assert
        _contaRepoMock.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>()), Times.Never);
        _outboxRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _contaRepoMock.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Crédito já realizado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}