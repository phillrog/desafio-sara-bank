using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
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
        var docRef = _db.Collection(NomeColecao).Document(movimentacao.Id.ToString());

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

        return snapshot.Documents.Select(doc => ToModel(doc));
    }
    

    public async Task<bool> ExisteMovimentacaoParaSagaAsync(Guid sagaId, string tipo)
    {
        Query query = _db.Collection(NomeColecao)
            .WhereEqualTo("SagaId", sagaId.ToString())
            .WhereEqualTo("Tipo", tipo)
            .Limit(1);

        QuerySnapshot snapshot = (UnitOfWorkFirestore?.TransacaoAtual != null)
            ? await UnitOfWorkFirestore.TransacaoAtual.GetSnapshotAsync(query)
            : await query.GetSnapshotAsync();

        return snapshot.Documents.Count > 0;
    }

    public async Task<bool> ExisteEstornoParaSagaAsync(Guid sagaId) 
        => await ExisteMovimentacaoParaSagaAsync(sagaId, "ESTORNO");

    private Movimentacao ToModel(DocumentSnapshot doc)
    {
        var dados = doc.ToDictionary();

        Guid id = Guid.Parse(doc.Id);
        Guid contaId = Guid.Parse(dados["ContaId"].ToString());
        decimal valor = Convert.ToDecimal(dados["Valor"]);
        string tipo = dados.ContainsKey("Tipo") ? dados["Tipo"].ToString() : string.Empty;
        string descricao = dados.ContainsKey("Descricao") ? dados["Descricao"].ToString() : string.Empty;

        Guid? sagaId = null;
        if (dados.ContainsKey("SagaId") && dados["SagaId"] != null)
        {
            sagaId = Guid.Parse(dados["SagaId"].ToString());
        }

        DateTime data = dados["Data"] is Timestamp ts ? ts.ToDateTime() : Convert.ToDateTime(dados["Data"]);

        return new Movimentacao(id, contaId, valor, tipo, descricao, data, sagaId);
    }
}