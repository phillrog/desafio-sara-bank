using Google.Cloud.Firestore;

namespace SaraBank.Infrastructure.Persistence
{
    
    public class FirestoreUnitOfWork : IUnitOfWork
    {
        private readonly FirestoreDb _db;
        
        private readonly AsyncLocal<Transaction?> _currentTransaction = new();

        public Transaction? TransacaoAtual => _currentTransaction.Value;

        public FirestoreUnitOfWork(FirestoreDb db) => _db = db;

        public async Task<T> ExecutarAsync<T>(Func<Task<T>> acao)
        {
            return await _db.RunTransactionAsync(async transaction =>
            {
                _currentTransaction.Value = transaction;
                try
                {
                    return await acao();
                }
                finally
                {
                    _currentTransaction.Value = null;
                }
            });
        }
    }
}
