using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;

namespace SaraBank.Infrastructure.Repositories;

public class MovimentacaoRepository : IMovimentacaoRepository
{
    private readonly FirestoreDb _db;
    private readonly IUnitOfWork _uow;

    public MovimentacaoRepository(FirestoreDb db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task AdicionarAsync(Movimentacao movimentacao)
    {
        var docRef = _db.Collection("Movimentacoes").Document(movimentacao.Id);

        if (_uow is FirestoreUnitOfWork fUow && fUow.TransacaoAtual != null)
        {
            fUow.TransacaoAtual.Set(docRef, movimentacao);
            await Task.CompletedTask;
        }
        else
        {
            await docRef.SetAsync(movimentacao);
        }
    }

    public Task<IEnumerable<Movimentacao>> ObterPorContaIdAsync(string contaId)
    {
        throw new NotImplementedException();
    }
}