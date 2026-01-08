using Moq;
using FluentAssertions;
using SaraBank.Domain.Entities;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Handlers.Commands;

namespace SaraBank.UnitTests.Application.Handlers;

public class DepositoTests
{
    [Fact]
    public async Task Deve_Depositar_Valor_Com_Sucesso_E_Atualizar_Saldo()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var conta = new ContaCorrente(usuarioId, 100m);
        var valorDeposito = 50m;

        var mockRepoConta = new Mock<IContaRepository>();
        var mockRepoMov = new Mock<IMovimentacaoRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        mockRepoConta.Setup(r => r.ObterPorIdAsync(conta.Id)).ReturnsAsync(conta);

        mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
               .Returns(async (Func<Task<bool>> acao) => await acao());

        var command = new DepositarCommand(conta.Id, valorDeposito);
        var handler = new DepositarHandler(mockRepoConta.Object, mockRepoMov.Object, mockUow.Object);

        // Act
        var resultado = await handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();
        conta.Saldo.Should().Be(150m);

        mockRepoConta.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Once);
        mockRepoMov.Verify(r => r.AdicionarAsync(It.IsAny<Movimentacao>()), Times.Once);
        mockUow.Verify(u => u.AdicionarAoOutboxAsync(It.IsAny<string>(), "DepositoRealizado"), Times.Once);
    }
}