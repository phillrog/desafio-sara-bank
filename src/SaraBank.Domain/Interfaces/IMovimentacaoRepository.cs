using SaraBank.Domain.Entities;

namespace SaraBank.Domain.Interfaces
{
    public interface IMovimentacaoRepository
    {
        Task AdicionarAsync(Movimentacao movimentacao);
        Task<IEnumerable<Movimentacao>> ObterPorContaIdAsync(string contaId);
    }
}
