namespace SaraBank.Application.Interfaces
{
    public interface IOutboxRepository
    {
        Task<IEnumerable<OutboxMessageDTO>> ObterNaoProcessadosAsync(int limite, CancellationToken ct);
        Task MarcarComoProcessadoAsync(string id, CancellationToken ct);
        Task AdicionarAsync(OutboxMessageDTO message, CancellationToken ct);
        Task IncrementarFalhaAsync(string id, CancellationToken ct);
    }

    public record OutboxMessageDTO(
        string Id,
        string Payload,
        string Tipo,
        int Tentativas = 0,
        bool Processado = false
    );
}
