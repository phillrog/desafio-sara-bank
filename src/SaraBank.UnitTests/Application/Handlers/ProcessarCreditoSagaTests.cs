using Microsoft.Extensions.Logging;
using Moq;
using SaraBank.Application.Events;
using SaraBank.Application.Handlers.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;

namespace SaraBank.UnitTests.Handlers;

public class ProcessarCreditoSagaHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IContaRepository> _contaRepoMock;
    private readonly Mock<IMovimentacaoRepository> _movRepoMock;
    private readonly Mock<ILogger<ProcessarCreditoSagaHandler>> _loggerMock;
    private readonly ProcessarCreditoSagaHandler _handler;

    public ProcessarCreditoSagaHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _contaRepoMock = new Mock<IContaRepository>();
        _movRepoMock = new Mock<IMovimentacaoRepository>();
        _loggerMock = new Mock<ILogger<ProcessarCreditoSagaHandler>>();

        // Setup para o Unit of Work sempre executar o callback passado a ele
        _uowMock.Setup(x => x.ExecutarAsync<bool>(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async (func) => await func());

        _handler = new ProcessarCreditoSagaHandler(
            _uowMock.Object,
            _contaRepoMock.Object,
            _movRepoMock.Object,
            _loggerMock.Object);
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

        // conta destino NÃO foi encontrada
        _contaRepoMock.Setup(r => r.ObterPorIdAsync(eventoDebito.ContaDestinoId))
                      .ReturnsAsync((ContaCorrente)null);

        // Act
        await _handler.Handle(eventoDebito, CancellationToken.None);

        // Assert
        // ocorreu "FalhaNoCredito"
        _uowMock.Verify(u => u.AdicionarAoOutboxAsync(
            It.Is<string>(s => s.Contains("FalhaNoCredito")),
            "FalhaNoCredito"
        ), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(sagaId.ToString())),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}