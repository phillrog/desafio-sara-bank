using Moq;
using FluentAssertions;
using SaraBank.Domain.Entities;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Handlers.Commands;

namespace SaraBank.UnitTests.Application.Handlers;

public class SaqueTests
{
    [Fact]
    public async Task Deve_Sacar_Valor_Quando_Houver_Saldo_Suficiente()
    {
        // Arrange
        var conta = new ContaCorrente(Guid.NewGuid(), 200m);
        var valorSaque = 150m;

        var mockRepoConta = new Mock<IContaRepository>();
        var mockRepoMov = new Mock<IMovimentacaoRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        mockRepoConta.Setup(r => r.ObterPorIdAsync(conta.Id)).ReturnsAsync(conta);
        mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
               .Returns(async (Func<Task<bool>> acao) => await acao());

        var command = new SacarCommand(conta.Id, valorSaque);
        var handler = new SacarHandler(mockRepoConta.Object, mockRepoMov.Object, mockUow.Object);

        // Act
        var resultado = await handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();
        conta.Saldo.Should().Be(50m);
        mockRepoMov.Verify(r => r.AdicionarAsync(It.Is<Movimentacao>(m => m.Tipo == "DEBITO")), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Permitir_Saque_Quando_Saldo_For_Insuficiente()
    {
        // Arrange
        var conta = new ContaCorrente(Guid.NewGuid(), 50m);
        var valorSaque = 100m;

        var mockRepoConta = new Mock<IContaRepository>();
        var mockRepoMov = new Mock<IMovimentacaoRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        mockRepoConta.Setup(r => r.ObterPorIdAsync(conta.Id)).ReturnsAsync(conta);
        mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
               .Returns(async (Func<Task<bool>> acao) => await acao());

        var command = new SacarCommand(conta.Id, valorSaque);
        var handler = new SacarHandler(mockRepoConta.Object, mockRepoMov.Object, mockUow.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        conta.Saldo.Should().Be(50m); // Saldo deve permanecer intacto
        mockRepoConta.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Never);
    }
}