using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaraBank.Application.Events;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Interfaces;
using SaraBank.Application.Handlers.Events;

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

    [Fact]
    public async Task Handle_Deve_Ignorar_Debito_Se_Ja_Foi_Processado_Anteriormente()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var evento = new TransferenciaIniciadaEvent(sagaId, Guid.NewGuid(), Guid.NewGuid(), 100m);

        // Simula que a movimentação de DÉBITO já existe para esta Saga
        _mockMovRepo.Setup(m => m.ExisteMovimentacaoParaSagaAsync(sagaId, "DEBITO"))
                    .ReturnsAsync(true);

        // Act
        await _handler.Handle(evento, CancellationToken.None);

        // Assert
        // Se já processou, não deve nem buscar a conta no repositório
        _mockContaRepo.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>()), Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Débito já realizado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Deve_Relancar_Excecao_Quando_Ocorre_Falha_Tecnica()
    {
        // Arrange
        var evento = new TransferenciaIniciadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m);

        // Força um erro de conexão no repositório
        _mockMovRepo.Setup(m => m.ExisteMovimentacaoParaSagaAsync(It.IsAny<Guid>(), "DEBITO"))
                    .ThrowsAsync(new Exception("Timeout Firestore"));

        // Act
        Func<Task> act = async () => await _handler.Handle(evento, CancellationToken.None);

        // Assert
        // O handler deve relançar para o Worker tentar novamente (Retry Policy)
        await act.Should().ThrowAsync<Exception>().WithMessage("Timeout Firestore");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Falha na infraestrutura")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}