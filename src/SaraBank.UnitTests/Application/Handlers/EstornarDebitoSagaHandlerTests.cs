using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaraBank.Application.Events;
using SaraBank.Application.Handlers.Events;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Interfaces;

namespace SaraBank.UnitTests.Application.Handlers;

public class EstornarDebitoSagaHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IContaRepository> _mockContaRepo;
    private readonly Mock<IMovimentacaoRepository> _mockMovimentacaoRepo;
    private readonly Mock<ILogger<EstornarDebitoSagaHandler>> _mockLogger;
    private readonly EstornarDebitoSagaHandler _handler;

    public EstornarDebitoSagaHandlerTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockContaRepo = new Mock<IContaRepository>();
        _mockMovimentacaoRepo = new Mock<IMovimentacaoRepository>();
        _mockLogger = new Mock<ILogger<EstornarDebitoSagaHandler>>();

        _mockUow.Setup(u => u.ExecutarAsync<bool>(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async (acao) => await acao());

        _handler = new EstornarDebitoSagaHandler(
            _mockUow.Object,
            _mockContaRepo.Object,
            _mockMovimentacaoRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_DeveEstornarValor_ComSucesso()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaOrigem = new ContaCorrente(contaOrigemId, 100m);
        var evento = new FalhaNoCreditoEvent(Guid.NewGuid(), contaOrigemId, 50m, "Erro");

        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaOrigemId)).ReturnsAsync(contaOrigem);

        _mockUow.Setup(u => u.ExecutarAsync<bool>(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async (func) => await func());

        // Act
        await _handler.Handle(evento, CancellationToken.None);

        // Assert
        contaOrigem.Saldo.Should().Be(150m);

        _mockUow.Verify(u => u.ExecutarAsync<bool>(It.IsAny<Func<Task<bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLogarErroCritico_QuandoContaOrigemNaoForEncontrada()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var evento = new FalhaNoCreditoEvent(Guid.NewGuid(), contaOrigemId, 50m, "Conta destino inválida");

        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaOrigemId))
                      .ReturnsAsync((ContaCorrente)null);

        // Act
        await _handler.Handle(evento, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("não encontrada para estorno")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockContaRepo.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Never);
        _mockMovimentacaoRepo.Verify(r => r.AdicionarAsync(It.IsAny<Movimentacao>()), Times.Never);
    }
}