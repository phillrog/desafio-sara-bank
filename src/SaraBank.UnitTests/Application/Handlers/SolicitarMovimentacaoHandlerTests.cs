using FluentAssertions;
using FluentValidation;
using Moq;
using SaraBank.Application.Commands;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Interfaces;
using Xunit;

namespace SaraBank.UnitTests.Application.Handlers;

public class SolicitarMovimentacaoHandlerTests
{
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
    private readonly SolicitarMovimentacaoHandler _handler;

    public SolicitarMovimentacaoHandlerTests()
    {
        _contaRepositoryMock = new Mock<IContaRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _outboxRepositoryMock = new Mock<IOutboxRepository>();

        _uowMock.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
                        .Returns((Func<Task<bool>> func) => func());

        _handler = new SolicitarMovimentacaoHandler(
            _uowMock.Object,
            _contaRepositoryMock.Object,
            _outboxRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoContaNaoExistir()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new SolicitarMovimentacaoCommand(contaId, 100, "Debito");

        _contaRepositoryMock.Setup(r => r.ObterPorIdAsync(contaId))
                            .ReturnsAsync((ContaCorrente)null!);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "ContaId"));

        // Garante que nada foi para o Outbox se a conta não existe
        _outboxRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveProcessarDebitoComSucesso_QuandoDadosForemValidos()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new SolicitarMovimentacaoCommand(contaId, 50, "Debito");
        var contaExistente = new ContaCorrente(contaId, Guid.NewGuid(), 1000);

        _contaRepositoryMock.Setup(r => r.ObterPorIdAsync(contaId))
                            .ReturnsAsync(contaExistente);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();

        // Verifica o repositório de Outbox e o tópico de movimentações
        _outboxRepositoryMock.Verify(r => r.AdicionarAsync(
            It.Is<OutboxMessage>(m =>
                m.Tipo == "NovaMovimentacao" &&
                m.Topico == "sara-bank-movimentacoes"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveProcessarCreditoComSucesso_QuandoDadosForemValidos()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new SolicitarMovimentacaoCommand(contaId, 500, "Credito");
        var contaExistente = new ContaCorrente(contaId, Guid.NewGuid(), 0);

        _contaRepositoryMock.Setup(r => r.ObterPorIdAsync(contaId))
                            .ReturnsAsync(contaExistente);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();

        // Verifica o repositório de Outbox
        _outboxRepositoryMock.Verify(r => r.AdicionarAsync(
            It.Is<OutboxMessage>(m =>
                m.Tipo == "NovaMovimentacao" &&
                m.Topico == "sara-bank-movimentacoes"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}