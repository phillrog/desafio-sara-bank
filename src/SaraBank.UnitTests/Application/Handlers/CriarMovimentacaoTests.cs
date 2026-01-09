using Moq;
using MediatR;
using SaraBank.Application.Commands;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Events;
using Xunit;

namespace SaraBank.UnitTests.Application.Handlers;

public class CriarMovimentacaoTests
{
    private readonly Mock<IContaRepository> _contaRepoMock;
    private readonly Mock<IOutboxRepository> _outboxRepoMock; // Mantido conforme solicitado, mas o Handler atual não usa mais para Pub/Sub
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMovimentacaoRepository> _movimentacaoRepository;
    private readonly Mock<IMediator> _mediatorMock; // Novo Mock para o Mediator
    private readonly CriarMovimentacaoHandler _handler;

    public CriarMovimentacaoTests()
    {
        _contaRepoMock = new Mock<IContaRepository>();
        _outboxRepoMock = new Mock<IOutboxRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _movimentacaoRepository = new Mock<IMovimentacaoRepository>();
        _mediatorMock = new Mock<IMediator>(); // Inicializando o Mock

        _uowMock.Setup(x => x.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> func) => func());

        // Atualizado com o Mediator e removendo dependências que não estão mais no construtor do Handler real
        _handler = new CriarMovimentacaoHandler(
            _contaRepoMock.Object,
            _outboxRepoMock.Object,
            _uowMock.Object,
            _movimentacaoRepository.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task Deve_Creditar_Valor_Na_Conta_E_Publicar_Evento_Quando_Credito_For_Valido()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var saldoInicial = 100m;
        var valorCredito = 50m;

        // Assumindo que sua entidade é 'Conta' conforme o Handler anterior
        var conta = new ContaCorrente(contaId, usuarioId, saldoInicial);

        var command = new CriarMovimentacaoCommand(contaId, valorCredito, "Credito");

        _contaRepoMock.Setup(x => x.ObterPorIdAsync(contaId))
                      .ReturnsAsync(conta);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(resultado);
        Assert.Equal(150m, conta.Saldo); // 100 + 50

        // Verifica se o repositório de conta foi atualizado
        _contaRepoMock.Verify(x => x.AtualizarAsync(conta), Times.Once);

        // VERIFICAÇÃO DO MEDIATOR: Garante que o evento de domínio foi disparado internamente
        _mediatorMock.Verify(x => x.Publish(
            It.Is<MovimentacaoRealizadaEvent>(e => e.ContaId == contaId && e.Valor == valorCredito),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deve_Retornar_False_Quando_Conta_Nao_Existir()
    {
        // Arrange
        var command = new CriarMovimentacaoCommand(Guid.NewGuid(), 50m, "Credito");
        _contaRepoMock.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>()))
                      .ReturnsAsync((ContaCorrente)null!);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(resultado);

        // Garante que não publicou evento se a conta não existe
        _mediatorMock.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Creditar_Valor_Na_Conta_E_Gerar_Movimentacao_E_Publicar_Evento_Quando_Credito_For_Valido()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var saldoInicial = 100m;
        var valorCredito = 50m;

        var conta = new ContaCorrente(contaId, usuarioId, saldoInicial);
        var command = new CriarMovimentacaoCommand(contaId, valorCredito, "Credito");

        _contaRepoMock.Setup(x => x.ObterPorIdAsync(contaId))
                      .ReturnsAsync(conta);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(resultado);
        Assert.Equal(150m, conta.Saldo);

        // Verifica se o repositório de conta foi atualizado
        _contaRepoMock.Verify(x => x.AtualizarAsync(conta), Times.Once);

        // Verifica se a movimentação física (extrato) foi gravada
        _movimentacaoRepository.Verify(x => x.AdicionarAsync(
            It.Is<Movimentacao>(m => m.ContaId == contaId && m.Valor == valorCredito && m.Tipo == "Credito")),
            Times.Once);

        // Verifica se o evento de domínio foi publicado
        _mediatorMock.Verify(x => x.Publish(It.IsAny<MovimentacaoRealizadaEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}