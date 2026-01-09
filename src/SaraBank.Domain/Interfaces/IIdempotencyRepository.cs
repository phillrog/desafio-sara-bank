namespace SaraBank.Domain.Interfaces
{
    public interface IIdempotencyRepository
    {
        Task<bool> ChaveJaExisteAsync(Guid chave);
        Task SalvarChaveAsync(Guid chave, string nomeComando);
    }
}
