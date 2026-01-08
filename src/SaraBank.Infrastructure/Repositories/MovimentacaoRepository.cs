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
    private const string NomeColecao = "Movimentacoes";

    public MovimentacaoRepository(FirestoreDb db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    private FirestoreUnitOfWork? UnitOfWorkFirestore => _uow as FirestoreUnitOfWork;

    public async Task AdicionarAsync(Movimentacao movimentacao)
    {
        var docRef = _db.Collection(NomeColecao).Document();

        if (UnitOfWorkFirestore?.TransacaoAtual != null)
        {
            UnitOfWorkFirestore.TransacaoAtual.Set(docRef, movimentacao);
        }
        else
        {
            await docRef.SetAsync(movimentacao);
        }
    }

    public async Task<IEnumerable<Movimentacao>> ObterPorContaIdAsync(string contaId)
    {
        Query query = _db.Collection(NomeColecao)
                         .WhereEqualTo("ContaId", contaId)
                         .OrderByDescending("Data");

        QuerySnapshot snapshot = (UnitOfWorkFirestore?.TransacaoAtual != null)
            ? await UnitOfWorkFirestore.TransacaoAtual.GetSnapshotAsync(query)
            : await query.GetSnapshotAsync();

        return snapshot.Documents.Select(doc => doc.ConvertTo<Movimentacao>());
    }
}