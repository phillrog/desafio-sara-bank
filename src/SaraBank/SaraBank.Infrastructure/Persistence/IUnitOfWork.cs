using Google.Cloud.Firestore;

namespace SaraBank.Infrastructure.Persistence
{
    public interface IUnitOfWork
    {
        Transaction? TransacaoAtual { get; }
        Task<T> ExecutarAsync<T>(Func<Task<T>> acao);
    }
}
