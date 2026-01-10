using SaraBank.Domain.Entities;

namespace SaraBank.Application.Interfaces
{
    public interface IOutboxRepository
    {
        Task<IEnumerable<OutboxMessage>> ObterNaoProcessadosAsync(int limite, CancellationToken ct);
        Task MarcarComoProcessadoAsync(Guid id, CancellationToken ct);
        Task AdicionarAsync(OutboxMessage message, CancellationToken ct);
        Task IncrementarFalhaAsync(Guid id, CancellationToken ct);
    }    
}
