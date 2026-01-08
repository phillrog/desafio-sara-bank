using SaraBank.Domain.Entities;

namespace SaraBank.Domain.Interfaces
{
    public interface IUsuarioRepository
    {
        Task AdicionarAsync(Usuario usuario);
        Task<Usuario?> ObterPorIdAsync(string id);
        Task<Usuario?> ObterPorCPFAsync(string cpf);
    }
}
