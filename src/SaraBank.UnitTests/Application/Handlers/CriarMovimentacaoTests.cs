using FluentValidation;
using Moq;
using SaraBank.Application.Commands;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;

namespace SaraBank.UnitTests.Application.Handlers;

public class CriarMovimentacaoTests
{
    private readonly Mock<IContaRepository> _contaRepoMock;
    private readonly Mock<IOutboxRepository> _outboxRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly CriarMovimentacaoHandler _handler;

    public CriarMovimentacaoTests()
    {
        _contaRepoMock = new Mock<IContaRepository>();
        _outboxRepoMock = new Mock<IOutboxRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _uowMock.Setup(x => x.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> func) => func());

        _handler = new CriarMovimentacaoHandler(
            _contaRepoMock.Object,
            _outboxRepoMock.Object,
            _uowMock.Object);
    }

    [Fact]
    public async Task Deve_Creditar_Valor_Na_Conta_E_Gerar_Outbox_Quando_Deposito_For_Valido()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var saldoInicial = 100m;
        var valorDeposito = 50m;

        var conta = new ContaCorrente(usuarioId, saldoInicial);

        var command = new CriarMovimentacaoCommand(contaId, valorDeposito, "Deposito");

        _contaRepoMock.Setup(x => x.ObterPorIdAsync(contaId))
                      .ReturnsAsync(conta);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(resultado);
        Assert.Equal(150m, conta.Saldo); // 100 + 50

        // Verifica se o repositório de conta foi atualizado
        _contaRepoMock.Verify(x => x.AtualizarAsync(conta), Times.Once);

        // Verifica se a mensagem foi adicionada à Outbox
        _outboxRepoMock.Verify(x => x.AdicionarAsync(
            It.Is<OutboxMessageDTO>(m => m.Tipo == "MovimentacaoRealizadaEvent"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deve_Retornar_False_Quando_Conta_Nao_Existir()
    {
        // Arrange
        var command = new CriarMovimentacaoCommand(It.IsAny<Guid>(), 50m, "Deposito");
        _contaRepoMock.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>()))
                      .ReturnsAsync((ContaCorrente)null);
        

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(resultado);
        _outboxRepoMock.Verify(x => x.AdicionarAsync(It.IsAny<OutboxMessageDTO>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}