using FluentAssertions;
using FluentValidation;
using Moq;
using SaraBank.Application.Commands;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Interfaces;

namespace SaraBank.Tests.Application.Handlers;

public class SolicitarMovimentacaoHandlerTests
{
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly SolicitarMovimentacaoHandler _handler;

    public SolicitarMovimentacaoHandlerTests()
    {
        _contaRepositoryMock = new Mock<IContaRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        // Ajuste do Mock para executar a função interna do Handler
        _uowMock.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> func) => func());

        _handler = new SolicitarMovimentacaoHandler(_uowMock.Object, _contaRepositoryMock.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Handle_DeveLancarValidationException_QuandoValorForInvalido(decimal valorInvalido)
    {
        // Arrange
        var command = new SolicitarMovimentacaoCommand(Guid.NewGuid(), valorInvalido, "Deposito");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Valor"));
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoContaNaoExistir()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new SolicitarMovimentacaoCommand(contaId, 100, "Saque");

        _contaRepositoryMock.Setup(r => r.ObterPorIdAsync(contaId))
                            .ReturnsAsync((ContaCorrente)null!);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "ContaId"));
    }

    [Fact]
    public async Task Handle_DeveProcessarSaqueComSucesso_QuandoDadosForemValidos()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new SolicitarMovimentacaoCommand(contaId, 50, "Saque");
        var contaExistente = new ContaCorrente(contaId, Guid.NewGuid(), 1000);

        _contaRepositoryMock.Setup(r => r.ObterPorIdAsync(contaId))
                            .ReturnsAsync(contaExistente);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();
        _uowMock.Verify(u => u.AdicionarAoOutboxAsync(It.IsAny<string>(), "NovaMovimentacao"), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveProcessarDepositoComSucesso_QuandoDadosForemValidos()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new SolicitarMovimentacaoCommand(contaId, 500, "Deposito");
        var contaExistente = new ContaCorrente(contaId, Guid.NewGuid(), 0);

        _contaRepositoryMock.Setup(r => r.ObterPorIdAsync(contaId))
                            .ReturnsAsync(contaExistente);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();
        _uowMock.Verify(u => u.AdicionarAoOutboxAsync(It.IsAny<string>(), "NovaMovimentacao"), Times.Once);
    }
}