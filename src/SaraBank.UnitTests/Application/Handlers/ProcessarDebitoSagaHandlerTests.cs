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
    private readonly Mock<IOutboxRepository> _mockOutboxRepo; // Ajustado nome para padrão
    private readonly ProcessarDebitoSagaHandler _handler;

    public ProcessarDebitoSagaHandlerTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockContaRepo = new Mock<IContaRepository>();
        _mockMovRepo = new Mock<IMovimentacaoRepository>();
        _mockLogger = new Mock<ILogger<ProcessarDebitoSagaHandler>>();
        _mockOutboxRepo = new Mock<IOutboxRepository>();

        // Simula o comportamento da transação (executa o que recebe)
        _mockUow.Setup(u => u.ExecutarAsync<bool>(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async (acao) => await acao());

        _mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(async (acao) => await acao());

        _handler = new ProcessarDebitoSagaHandler(
            _mockUow.Object,
            _mockContaRepo.Object,
            _mockMovRepo.Object,
            _mockLogger.Object,
            _mockOutboxRepo.Object);
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

        // Variável para capturar o que foi enviado
        OutboxMessage mensagemEnviada = null;
        _mockOutboxRepo
            .Setup(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((m, ct) => mensagemEnviada = m)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(eventoIniciado, CancellationToken.None);

        // Assert
        contaOrigem.Saldo.Should().Be(800m);
        _mockContaRepo.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Once);

        // Se o verify falhar, inspecione a variável 'mensagemEnviada' no Debug
        mensagemEnviada.Should().NotBeNull("O Handler deveria ter chamado o AdicionarAsync do Outbox");
        mensagemEnviada.Tipo.Should().Be("SaldoDebitado");
        mensagemEnviada.Topico.Should().Be("sara-bank-transferencias-debitadas");
    }

    [Fact]
    public async Task Handle_DeveCancelarSaga_QuandoSaldoForInsuficiente()
    {
        // Arrange
        var contaOrigem = new ContaCorrente(Guid.NewGuid(), 50m);
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
        contaOrigem.Saldo.Should().Be(50m);

        //
        _mockOutboxRepo.Verify(r => r.AdicionarAsync(
            It.Is<OutboxMessage>(m =>
                m.Tipo == "TransferenciaCancelada" &&
                m.Topico == "sara-bank-transferencias-erros"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockContaRepo.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Deve_Ignorar_Debito_Se_Ja_Foi_Processado_Anteriormente()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var evento = new TransferenciaIniciadaEvent(sagaId, Guid.NewGuid(), Guid.NewGuid(), 100m);

        _mockMovRepo.Setup(m => m.ExisteMovimentacaoParaSagaAsync(sagaId, "DEBITO"))
                    .ReturnsAsync(true);

        // Act
        await _handler.Handle(evento, CancellationToken.None);

        // Assert
        _mockContaRepo.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockOutboxRepo.Verify(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);

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

        _mockMovRepo.Setup(m => m.ExisteMovimentacaoParaSagaAsync(It.IsAny<Guid>(), "DEBITO"))
                    .ThrowsAsync(new Exception("Timeout Firestore"));

        // Act
        Func<Task> act = async () => await _handler.Handle(evento, CancellationToken.None);

        // Assert
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