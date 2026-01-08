using SaraBank.Domain.Entities;

namespace SaraBank.Domain.Interfaces
{
    public interface IContaRepository
    {
        Task AdicionarAsync(ContaCorrente conta);
        Task<ContaCorrente> ObterPorIdAsync(string id);
        Task<ContaCorrente?> ObterPorUsuarioIdAsync(Guid usuarioId);
        Task AtualizarAsync(ContaCorrente conta);
        Task<bool> VerificarIdempotenciaAsync(Guid chave);
        Task RegistrarChaveIdempotenciaAsync(Guid chave);
    }
}
