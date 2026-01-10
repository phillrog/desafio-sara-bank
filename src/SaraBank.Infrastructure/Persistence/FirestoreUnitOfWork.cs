using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;

namespace SaraBank.Infrastructure.Persistence;

public class FirestoreUnitOfWork : IUnitOfWork
{
    private readonly FirestoreDb _db;
    private readonly AsyncLocal<Transaction?> _transacaoAtual = new();

    public FirestoreUnitOfWork(FirestoreDb db)
    {
        _db = db;
    }
    public Transaction? TransacaoAtual => _transacaoAtual.Value;

    public async Task<T> ExecutarAsync<T>(Func<Task<T>> acao)
    {
        return await _db.RunTransactionAsync(async transaction =>
        {
            _transacaoAtual.Value = transaction;
            try
            {
                return await acao();
            }
            finally
            {
                _transacaoAtual.Value = null;
            }
        });
    }

    public async Task ExecutarAsync(Func<Task> acao)
    {
        await _db.RunTransactionAsync(async transaction =>
        {
            _transacaoAtual.Value = transaction;
            try
            {
                await acao();
            }
            finally
            {
                _transacaoAtual.Value = null;
            }
        });
    }
}