using Moq;
using FluentAssertions;
using SaraBank.Domain.Entities;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Handlers.Commands;
using Xunit;

namespace SaraBank.UnitTests.Application.Handlers;

public class RealizarTransferenciaTests
{
    private readonly Mock<IContaRepository> _mockContaRepo;
    private readonly Mock<IMovimentacaoRepository> _mockMovimentacaoRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IOutboxRepository> _mockOutboxRepo;
    private readonly RealizarTransferenciaHandler _handler;

    public RealizarTransferenciaTests()
    {
        _mockContaRepo = new Mock<IContaRepository>();
        _mockMovimentacaoRepo = new Mock<IMovimentacaoRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockOutboxRepo = new Mock<IOutboxRepository>();

        _mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns(async (Func<Task<bool>> acao) => await acao());

        _handler = new RealizarTransferenciaHandler(
            _mockContaRepo.Object,
            _mockMovimentacaoRepo.Object,
            _mockUow.Object,
            _mockOutboxRepo.Object);
    }

    [Fact]
    public async Task Deve_Iniciar_Saga_Com_Sucesso_Quando_Contas_Existem()
    {
        // Arrange
        var contaOrigem = new ContaCorrente(Guid.NewGuid(), 500m);
        var contaDestino = new ContaCorrente(Guid.NewGuid(), 100m);
        var command = new RealizarTransferenciaCommand(contaOrigem.Id, contaDestino.Id, 200m);

        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaOrigem.Id)).ReturnsAsync(contaOrigem);
        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaDestino.Id)).ReturnsAsync(contaDestino);

        OutboxMessage mensagemCapturada = null;
        _mockOutboxRepo
            .Setup(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((m, ct) => mensagemCapturada = m)
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();

        // Se falhar aqui, o log vai mostrar o que o Handler realmente criou
        mensagemCapturada.Should().NotBeNull("O evento de Outbox não foi gerado. O Handler pode ter retornado erro antes.");
        mensagemCapturada.Tipo.Should().Be("TransferenciaIniciada");
        mensagemCapturada.Topico.Should().Be("sara-bank-transferencias-iniciadas");
    }
}