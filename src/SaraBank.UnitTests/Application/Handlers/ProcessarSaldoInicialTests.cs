using FluentAssertions;
using Moq;
using SaraBank.Application.Events;
using SaraBank.Application.Handlers.Events;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using System.Text.Json;
using Xunit;

namespace SaraBank.UnitTests.Application;

public class ProcessarSaldoInicialHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IOutboxRepository> _outboxRepoMock;
    private readonly ProcessarSaldoInicialHandler _handler;

    public ProcessarSaldoInicialHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _outboxRepoMock = new Mock<IOutboxRepository>();

        _uowMock.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> func) => func());

        _handler = new ProcessarSaldoInicialHandler(_uowMock.Object, _outboxRepoMock.Object);
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
        _outboxRepoMock.Verify(x => x.AdicionarAsync(
            It.Is<OutboxMessage>(m =>
                m.Tipo == "NovaMovimentacao" &&
                m.Topico == "sara-bank-movimentacoes"),
            It.IsAny<CancellationToken>()),
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
            SaldoInicial: 0m,
            DataCriacao: DateTime.UtcNow
        );

        // Act
        await _handler.Handle(eventoEntrada, CancellationToken.None);

        // Assert
        _outboxRepoMock.Verify(x => x.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
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

        OutboxMessage? mensagemCapturada = null;
        _outboxRepoMock.Setup(x => x.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
                       .Callback<OutboxMessage, CancellationToken>((m, ct) => mensagemCapturada = m);

        // Act
        await _handler.Handle(eventoEntrada, CancellationToken.None);

        // Assert
        mensagemCapturada.Should().NotBeNull();
        var envelope = JsonDocument.Parse(mensagemCapturada!.Payload);

        Assert.Equal("NovaMovimentacao", mensagemCapturada.Tipo);
        Assert.Equal("sara-bank-movimentacoes", mensagemCapturada.Topico);

        // Se o seu payload for o JSON direto da movimentação:
        var root = envelope.RootElement;
        if (root.TryGetProperty("Payload", out var innerPayloadStr))
        {
            var payloadInterno = JsonDocument.Parse(innerPayloadStr.GetString()!);
            Assert.Equal(contaId.ToString(), payloadInterno.RootElement.GetProperty("ContaId").GetString());
            Assert.Equal(saldo, payloadInterno.RootElement.GetProperty("Valor").GetDecimal());
        }
        else
        {
            // Caso o payload seja plano
            Assert.Equal(contaId.ToString(), root.GetProperty("ContaId").GetString());
            Assert.Equal(saldo, root.GetProperty("Valor").GetDecimal());
        }
    }
}