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

    [Fact]
    public async Task Handle_NaoDeveEstornarNovamente_SeJaFoiProcessado()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();
        var contaOrigem = new ContaCorrente(contaOrigemId, 100m);
        var evento = new FalhaNoCreditoEvent(sagaId, contaOrigemId, 50m, "Erro");

        // Simula que a busca no repositório encontrou um estorno já existente
        _mockMovimentacaoRepo.Setup(r => r.ExisteEstornoParaSagaAsync(sagaId))
                             .ReturnsAsync(true);

        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaOrigemId))
                      .ReturnsAsync(contaOrigem);

        // Act
        await _handler.Handle(evento, CancellationToken.None);

        // Assert
        // O saldo deve permanecer o mesmo, pois o estorno foi ignorado (idempotência)
        contaOrigem.Saldo.Should().Be(100m);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Estorno já realizado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockContaRepo.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRelancarExcecao_QuandoOcorreErroTecnico()
    {
        // Arrange
        var evento = new FalhaNoCreditoEvent(Guid.NewGuid(), Guid.NewGuid(), 50m, "Erro");

        // Força uma exceção
        _mockMovimentacaoRepo.Setup(r => r.ExisteEstornoParaSagaAsync(It.IsAny<Guid>()))
                             .ThrowsAsync(new Exception("Falha de conexão com Firestore"));

        // Act
        Func<Task> act = async () => await _handler.Handle(evento, CancellationToken.None);

        // Assert
        // O Handler deve dar "throw"
        await act.Should().ThrowAsync<Exception>().WithMessage("Falha de conexão com Firestore");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Falha ao estornar Saga")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}