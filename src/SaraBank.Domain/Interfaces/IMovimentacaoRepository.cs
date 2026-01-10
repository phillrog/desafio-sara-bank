using SaraBank.Domain.Entities;

namespace SaraBank.Domain.Interfaces
{
    public interface IMovimentacaoRepository
    {
        Task AdicionarAsync(Movimentacao movimentacao);
        Task<IEnumerable<Movimentacao>> ObterPorContaIdAsync(string contaId);

        Task<bool> ExisteEstornoParaSagaAsync(Guid sagaId);

        Task<bool> ExisteMovimentacaoParaSagaAsync(Guid sagaId, string tipo);        
    }
}
