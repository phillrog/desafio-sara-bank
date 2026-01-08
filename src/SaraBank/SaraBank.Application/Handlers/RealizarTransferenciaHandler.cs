using Google.Cloud.Firestore;
using MediatR;
using SaraBank.Application.Commands;

namespace SaraBank.Application.Handlers
{
    public class RealizarTransferenciaHandler : IRequestHandler<RealizarTransferenciaCommand, bool>
    {
        private readonly FirestoreDb _firestore;

        public RealizarTransferenciaHandler(FirestoreDb firestore) => _firestore = firestore;

        public async Task<bool> Handle(RealizarTransferenciaCommand request, CancellationToken ct)
        {
            return await _firestore.RunTransactionAsync(async transaction =>
            {
                var docOrigem = _firestore.Collection("Contas").Document(request.ContaOrigemId);
                var docDestino = _firestore.Collection("Contas").Document(request.ContaDestinoId);

                var snapshotOrigem = await transaction.GetSnapshotAsync(docOrigem);
                var snapshotDestino = await transaction.GetSnapshotAsync(docDestino);

                var saldoOrigem = snapshotOrigem.GetValue<decimal>("Saldo");
                if (saldoOrigem < request.Valor) throw new Exception("Saldo insuficiente.");

                // Atualiza saldos
                transaction.Update(docOrigem, "Saldo", saldoOrigem - request.Valor);
                transaction.Update(docDestino, "Saldo", snapshotDestino.GetValue<decimal>("Saldo") + request.Valor);

                // Transactional Outbox: Salva o evento na mesma transação
                var outboxRef = _firestore.Collection("Outbox").Document();
                transaction.Create(outboxRef, new
                {
                    Id = Guid.NewGuid(),
                    Tipo = "TransferenciaRealizada",
                    Payload = request,
                    DataCriacao = Timestamp.GetCurrentTimestamp(),
                    Processado = false
                });

                return true;
            });
        }
    }
}