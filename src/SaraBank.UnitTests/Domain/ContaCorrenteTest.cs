using FluentAssertions;
using SaraBank.Domain.Entities;

namespace SaraBank.UnitTests.Domain
{
    public class ContaCorrenteTest
    {
        [Fact]
        [Trait("Categoria", "Domínio")]
        public void Deve_Realizar_Transferencia_Com_Sucesso_Entre_Duas_Contas()
        {
            // Arrange
            var usuarioA = new Usuario("João Silva", "123.456.789-00", "joao@email.com");
            var usuarioB = new Usuario("Maria Souza", "987.654.321-11", "maria@email.com");

            var contaOrigem = new ContaCorrente(usuarioA.Id, 1000m);
            var contaDestino = new ContaCorrente(usuarioB.Id, 0m);
            decimal valorTransferencia = 400m;

            // Act
            contaOrigem.Sacar(valorTransferencia);
            contaDestino.Depositar(valorTransferencia);

            // Assert
            contaOrigem.Saldo.Should().Be(600m, "O saldo da conta de origem deve ser subtraído");
            contaDestino.Saldo.Should().Be(400m, "O saldo da conta de destino deve ser acrescido");
        }

        [Fact]
        public void Nao_Deve_Permitir_Debito_Se_Saldo_For_Insuficiente()
        {
            // Arrange
            var conta = new ContaCorrente(Guid.NewGuid(), 100m);

            // Act
            Action agir = () => conta.Sacar(150m);

            // Assert
            agir.Should().Throw<InvalidOperationException>()
                .WithMessage("Saldo insuficiente.");
        }
    }
}
