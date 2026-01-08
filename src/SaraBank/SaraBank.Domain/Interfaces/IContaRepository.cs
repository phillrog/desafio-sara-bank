using SaraBank.Domain.Entities;

namespace SaraBank.Domain.Interfaces
{
    public interface IContaRepository
    {
        Task<ContaCorrente> ObterPorIdAsync(string id);
        Task AtualizarAsync(ContaCorrente conta);
        Task<bool> VerificarIdempotenciaAsync(Guid chave);
        Task RegistrarChaveIdempotenciaAsync(Guid chave);
    }
}
