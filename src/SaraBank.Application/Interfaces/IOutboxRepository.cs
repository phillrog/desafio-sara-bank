namespace SaraBank.Application.Interfaces
{
    public interface IOutboxRepository
    {
        Task<IEnumerable<OutboxMessage>> ObterNaoProcessadosAsync(int limite, CancellationToken ct);
        Task MarcarComoProcessadoAsync(string id, CancellationToken ct);
    }

    public record OutboxMessage(string Id, string Payload, string Tipo);
}
