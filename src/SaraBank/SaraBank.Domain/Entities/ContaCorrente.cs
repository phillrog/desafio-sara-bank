namespace SaraBank.Domain.Entities
{
    public class ContaCorrente
    {
        public string Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public decimal Saldo { get; private set; }

        public ContaCorrente(Guid usuarioId, decimal saldoInicial = 0)
        {
            Id = Guid.NewGuid().ToString();
            UsuarioId = usuarioId;
            Saldo = saldoInicial;
        }

        public void Depositar(decimal valor)
        {
            if (valor <= 0) throw new ArgumentException("O valor do depósito deve ser positivo.");
            Saldo += valor;
        }

        public void Sacar(decimal valor)
        {
            if (valor <= 0) throw new ArgumentException("O valor do saque deve ser positivo.");
            if (Saldo < valor) throw new InvalidOperationException("Saldo insuficiente.");
            Saldo -= valor;
        }
    }
}
