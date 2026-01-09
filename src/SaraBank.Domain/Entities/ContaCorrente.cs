using Google.Cloud.Firestore;

namespace SaraBank.Domain.Entities
{
    [FirestoreData]
    public class ContaCorrente
    {
        [FirestoreDocumentId]
        public Guid Id { get; private set; }
        [FirestoreProperty]
        public Guid UsuarioId { get; private set; }
        [FirestoreProperty]
        public decimal Saldo { get; private set; }

        public ContaCorrente() {}
        public ContaCorrente(Guid usuarioId, decimal saldoInicial = 0)
        {
            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            Saldo = saldoInicial;
        }

        public ContaCorrente(Guid id, Guid usuarioId, decimal saldoInicial = 0)
        {
            Id = id;
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
            if (valor <= 0) throw new ArgumentException("O valor do Debito deve ser positivo.");
            if (Saldo < valor) throw new InvalidOperationException("Saldo insuficiente.");
            Saldo -= valor;
        }

        public void Debitar(decimal valor)
        {
            if (valor <= 0) throw new ArgumentException("O valor do débito deve ser positivo.");
            if (Saldo < valor) throw new InvalidOperationException("Saldo insuficiente.");

            Saldo -= valor;
        }

        public void Creditar(decimal valor)
        {
            if (valor <= 0) throw new ArgumentException("O valor do crédito deve ser positivo.");

            Saldo += valor;
        }
    }
}
