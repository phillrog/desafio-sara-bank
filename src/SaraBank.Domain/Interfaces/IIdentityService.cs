namespace SaraBank.Domain.Interfaces
{
    public interface IIdentityService
    {
        Task CriarUsuarioAsync(Guid id, string email, string senha, string nome);
        Task DeletarUsuarioAsync(Guid id);
        Task<string> AutenticarAsync(string email, string senha);
    }
}
