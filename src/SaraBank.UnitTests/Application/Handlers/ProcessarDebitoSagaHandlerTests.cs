using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaraBank.Application.Events;
using SaraBank.Application.Handlers.Events;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Interfaces;

namespace SaraBank.UnitTests.Application.Handlers;

public class ProcessarDebitoSagaHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IContaRepository> _mockContaRepo;
    private readonly Mock<IMovimentacaoRepository> _mockMovRepo;
    private readonly Mock<ILogger<ProcessarDebitoSagaHandler>> _mockLogger;
    private readonly ProcessarDebitoSagaHandler _handler;

    public ProcessarDebitoSagaHandlerTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockContaRepo = new Mock<IContaRepository>();
        _mockMovRepo = new Mock<IMovimentacaoRepository>();
        _mockLogger = new Mock<ILogger<ProcessarDebitoSagaHandler>>();

        _mockUow.Setup(u => u.ExecutarAsync<bool>(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async (acao) => await acao());

        _mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(async (acao) => await acao());

        _handler = new ProcessarDebitoSagaHandler(
            _mockUow.Object,
            _mockContaRepo.Object,
            _mockMovRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_DeveDebitarESeguirSaga_QuandoHaSaldoSuficiente()
    {
        // Arrange
        var contaOrigem = new ContaCorrente(Guid.NewGuid(), 1000m);
        var eventoIniciado = new TransferenciaIniciadaEvent(
            SagaId: Guid.NewGuid(),
            ContaOrigemId: contaOrigem.Id,
            ContaDestinoId: Guid.NewGuid(),
            Valor: 200m
        );

        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaOrigem.Id)).ReturnsAsync(contaOrigem);

        // Act
        await _handler.Handle(eventoIniciado, CancellationToken.None);

        // Assert
        contaOrigem.Saldo.Should().Be(800m); // 1000 - 200

        _mockContaRepo.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Once);

        _mockUow.Verify(u => u.AdicionarAoOutboxAsync(
            It.Is<string>(s => s.Contains("SaldoDebitado")),
            "SaldoDebitado"), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveCancelarSaga_QuandoSaldoForInsuficiente()
    {
        // Arrange
        var contaOrigem = new ContaCorrente(Guid.NewGuid(), 50m); // Saldo baixo
        var eventoIniciado = new TransferenciaIniciadaEvent(
            SagaId: Guid.NewGuid(),
            ContaOrigemId: contaOrigem.Id,
            ContaDestinoId: Guid.NewGuid(),
            Valor: 200m // Valor alto
        );

        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaOrigem.Id)).ReturnsAsync(contaOrigem);

        // Act
        await _handler.Handle(eventoIniciado, CancellationToken.None);

        // Assert
        contaOrigem.Saldo.Should().Be(50m); // Saldo não pode mudar

        // Verifica se disparou o evento de CANCELAMENTO por erro de negócio
        _mockUow.Verify(u => u.AdicionarAoOutboxAsync(
            It.Is<string>(s => s.Contains("TransferenciaCancelada")),
            "TransferenciaCancelada"), Times.Once);

        // Não deve ter atualizado saldo nem criado movimentação de débito
        _mockContaRepo.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Never);
    }
}