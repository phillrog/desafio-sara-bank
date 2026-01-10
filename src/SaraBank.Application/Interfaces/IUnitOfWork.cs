namespace SaraBank.Application.Interfaces
{
    public interface IUnitOfWork
    {        
        Task<T> ExecutarAsync<T>(Func<Task<T>> acao);
        Task ExecutarAsync(Func<Task> acao);
        Task AdicionarAoOutboxAsync(string payload, string tipo);
    }
}
