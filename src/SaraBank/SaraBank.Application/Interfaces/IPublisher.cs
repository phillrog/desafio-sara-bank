namespace SaraBank.Application.Interfaces
{
    public interface IPublisher
    {
        Task<string> PublicarAsync(string payload, CancellationToken ct = default);
    }
}
