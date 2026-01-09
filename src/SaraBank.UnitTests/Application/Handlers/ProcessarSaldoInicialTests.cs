using Moq;
using SaraBank.Application.Events;
using SaraBank.Application.Interfaces;
using System.Text.Json;

namespace SaraBank.UnitTests.Application;

public class ProcessarSaldoInicialHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ProcessarSaldoInicialHandler _handler;

    public ProcessarSaldoInicialHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _handler = new ProcessarSaldoInicialHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Deve_Adicionar_Ao_Outbox_Quando_SaldoInicial_For_Maior_Que_Zero()
    {
        // Arrange
        var eventoEntrada = new UsuarioCadastradoEvent(
            UsuarioId: Guid.NewGuid(),
            Nome: "Teste",
            Email: "teste@sara.com",
            ContaId: Guid.NewGuid(),
            SaldoInicial: 100m,
            DataCriacao: DateTime.UtcNow
        );

        // Act
        await _handler.Handle(eventoEntrada, CancellationToken.None);

        // Assert
        _uowMock.Verify(x => x.AdicionarAoOutboxAsync(
            It.Is<string>(s => s.Contains("NovaMovimentacao")),
            "NovaMovimentacao"),
            Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Adicionar_Ao_Outbox_Quando_SaldoInicial_For_Zero()
    {
        // Arrange
        var eventoEntrada = new UsuarioCadastradoEvent(
            UsuarioId: Guid.NewGuid(),
            Nome: "Teste",
            Email: "teste@sara.com",
            ContaId: Guid.NewGuid(),
            SaldoInicial: 0m, // Saldo Zero
            DataCriacao: DateTime.UtcNow
        );

        // Act
        await _handler.Handle(eventoEntrada, CancellationToken.None);

        // Assert
        _uowMock.Verify(x => x.AdicionarAoOutboxAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Gerar_Payload_Com_Dados_Corretos_No_Outbox()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var saldo = 50m;
        var eventoEntrada = new UsuarioCadastradoEvent(
            UsuarioId: Guid.NewGuid(),
            Nome: "Sara",
            Email: "sara@bank.com",
            ContaId: contaId,
            SaldoInicial: saldo,
            DataCriacao: DateTime.UtcNow
        );

        string payloadCapturado = string.Empty;
        _uowMock.Setup(x => x.AdicionarAoOutboxAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((p, t) => payloadCapturado = p);

        // Act
        await _handler.Handle(eventoEntrada, CancellationToken.None);

        // Assert
        var envelope = JsonDocument.Parse(payloadCapturado);
        var payloadInterno = JsonDocument.Parse(envelope.RootElement.GetProperty("Payload").GetString());

        Assert.Equal("NovaMovimentacao", envelope.RootElement.GetProperty("TipoEvento").GetString());
        Assert.Equal(contaId.ToString(), payloadInterno.RootElement.GetProperty("ContaId").GetString());
        Assert.Equal(saldo, payloadInterno.RootElement.GetProperty("Valor").GetDecimal());
    }
}