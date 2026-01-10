using Moq;
using SaraBank.Application.Commands;
using MediatR;
using SaraBank.Application.Handlers.Events;

namespace SaraBank.UnitTests.Application;

public class GravarMovimentacaoNoBancoHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GravarMovimentacaoNoBancoHandler _handler;

    public GravarMovimentacaoNoBancoHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _handler = new GravarMovimentacaoNoBancoHandler(_mediatorMock.Object);
    }

    [Fact]
    public async Task Deve_Converter_Evento_Em_Comando_E_Enviar_Para_O_Mediator()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var valor = 250.50m;
        var tipo = "Credito";
        var descricao = "Saldo Inicial";

        var evento = new NovaMovimentacaoEvent(contaId, valor, tipo, descricao);

        // Act
        await _handler.Handle(evento, CancellationToken.None);

        // Assert
        // Verifica se o Mediator.Send foi chamado com o comando contendo os dados do evento
        _mediatorMock.Verify(x => x.Send(
            It.Is<CriarMovimentacaoCommand>(c =>
                c.ContaId == contaId &&
                c.Valor == valor &&
                c.Tipo == tipo),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}