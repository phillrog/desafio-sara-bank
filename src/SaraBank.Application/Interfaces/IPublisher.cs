namespace SaraBank.Application.Interfaces
{
    public interface IPublisher
    {
        Task<string> PublicarAsync(string payload, string topico, CancellationToken ct = default);
    }
}
