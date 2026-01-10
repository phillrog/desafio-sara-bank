using FluentAssertions;
using SaraBank.Domain.Entities;
using Xunit;

namespace SaraBank.UnitTests.Domain;

public class MovimentacaoTests
{
    [Fact]
    public void ConstrutorComum_DeveGerarMovimentacaoSemSaga()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var valor = 50.00m;

        // Act
        var mov = new Movimentacao(contaId, valor, "DEBITO", "Pagamento Teste");

        // Assert
        mov.Id.Should().NotBeEmpty();
        mov.SagaId.Should().BeNull();
        mov.Data.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        mov.Tipo.Should().Be("DEBITO");
    }

    [Fact]
    public void ConstrutorSaga_DeveVincularSagaIdCorretamente()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();

        // Act
        var mov = new Movimentacao(contaId, 100m, "CREDITO", "Transferencia Saga", sagaId);

        // Assert
        mov.SagaId.Should().Be(sagaId);
        mov.SagaId.Should().NotBeNull();
    }

    [Fact]
    public void ConstrutorMestre_DevePermitirReconstruirObjetoDoBanco()
    {
        // Arrange
        var idExistente = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var dataPassada = DateTime.UtcNow.AddDays(-1);
        var sagaId = Guid.NewGuid();

        // Act
        var mov = new Movimentacao(idExistente, contaId, 75m, "ESTORNO", "Desc", dataPassada, sagaId);

        // Assert
        mov.Id.Should().Be(idExistente);
        mov.Data.Should().Be(dataPassada);
        mov.SagaId.Should().Be(sagaId);
    }
}