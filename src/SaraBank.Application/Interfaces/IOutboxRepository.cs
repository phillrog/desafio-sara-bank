namespace SaraBank.Application.Interfaces
{
    public interface IOutboxRepository
    {
        Task<IEnumerable<OutboxMessageDTO>> ObterNaoProcessadosAsync(int limite, CancellationToken ct);
        Task MarcarComoProcessadoAsync(string id, CancellationToken ct);
        Task AdicionarAsync(OutboxMessageDTO message, CancellationToken ct);
    }

    public record OutboxMessageDTO(string Id, string Payload, string Tipo);
}
