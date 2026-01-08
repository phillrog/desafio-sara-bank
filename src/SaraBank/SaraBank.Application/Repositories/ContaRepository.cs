using Google.Cloud.Firestore;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;
using SaraBank.Infrastructure.Repositories;

namespace SaraBank.Application.Repositories
{
    public class ContaRepository : RepositoryBase, IContaRepository
    {
        public ContaRepository(IUnitOfWork uow, FirestoreDb db) : base(uow, db) { }

        public async Task<ContaCorrente> ObterPorIdAsync(string id)
        {
            var docRef = _db.Collection("Contas").Document(id);

            DocumentSnapshot snapshot = _uow.TransacaoAtual != null
                ? await _uow.TransacaoAtual.GetSnapshotAsync(docRef)
                : await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) throw new Exception("Conta não encontrada.");

            return new ContaCorrente(
                snapshot.GetValue<Guid>("UsuarioId"),
                snapshot.GetValue<decimal>("Saldo")
            );
        }

        public async Task AtualizarAsync(ContaCorrente conta)
        {
            var docRef = _db.Collection("Contas").Document(conta.Id);
            var dados = new Dictionary<string, object> { { "Saldo", conta.Saldo } };

            if (_uow.TransacaoAtual != null)
                _uow.TransacaoAtual.Update(docRef, dados);
            else
                await docRef.UpdateAsync(dados);
        }

        public async Task<bool> VerificarIdempotenciaAsync(Guid chave)
        {
            var docRef = _db.Collection("Idempotencia").Document(chave.ToString());
            var snapshot = _uow.TransacaoAtual != null
                ? await _uow.TransacaoAtual.GetSnapshotAsync(docRef)
                : await docRef.GetSnapshotAsync();

            return snapshot.Exists;
        }

        public async Task RegistrarChaveIdempotenciaAsync(Guid chave)
        {
            var docRef = _db.Collection("Idempotencia").Document(chave.ToString());
            var dados = new { DataProcessamento = Timestamp.GetCurrentTimestamp() };

            if (_uow.TransacaoAtual != null)
                _uow.TransacaoAtual.Create(docRef, dados);
            else
                await docRef.CreateAsync(dados);
        }
    }
}