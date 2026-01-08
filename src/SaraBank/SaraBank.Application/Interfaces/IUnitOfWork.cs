namespace SaraBank.Application.Interfaces
{
    public interface IUnitOfWork
    {        
        Task<T> ExecutarAsync<T>(Func<Task<T>> acao);
        Task AdicionarAoOutboxAsync(string payload, string tipo);
    }
}
