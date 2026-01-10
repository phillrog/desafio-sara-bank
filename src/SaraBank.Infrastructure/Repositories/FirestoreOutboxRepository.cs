using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Infrastructure.Persistence;

namespace SaraBank.Infrastructure.Repositories;

public class FirestoreOutboxRepository : IOutboxRepository
{
    private readonly FirestoreDb _db;
    private readonly IUnitOfWork _uow;
    private const string NomeColecao = "Outbox";

    public FirestoreOutboxRepository(FirestoreDb db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    private FirestoreUnitOfWork? UnitOfWorkFirestore => _uow as FirestoreUnitOfWork;

    public async Task<IEnumerable<OutboxMessage>> ObterNaoProcessadosAsync(int limite, CancellationToken ct)
    {
        var collection = _db.Collection(NomeColecao);

        var query = collection
            .WhereEqualTo("Processado", false)
            .WhereLessThan("Tentativas", 5)
            .Limit(limite);

        var snapshot = await query.GetSnapshotAsync(ct);

        return snapshot.Documents.Select(doc => ToModel(doc));
    }

    public async Task IncrementarFalhaAsync(Guid id, CancellationToken ct)
    {
        var docRef = _db.Collection(NomeColecao).Document(id.ToString());

        var atualizacoes = new Dictionary<string, object>
        {
            { "Tentativas", FieldValue.Increment(1) },
            { "UltimaFalhaEm", Timestamp.FromDateTime(DateTime.UtcNow) }
        };

        await docRef.UpdateAsync(atualizacoes, cancellationToken: ct);
    }

    public async Task MarcarComoProcessadoAsync(Guid id, CancellationToken ct)
    {
        var docRef = _db.Collection(NomeColecao).Document(id.ToString());
        await docRef.UpdateAsync("Processado", true, cancellationToken: ct);
    }

    public async Task AdicionarAsync(OutboxMessage message, CancellationToken ct)
    {
        var docRef = _db.Collection(NomeColecao).Document(message.Id.ToString());

        var dados = new Dictionary<string, object>
        {
            { "Payload", message.Payload },
            { "Tipo", message.Tipo },
            { "Topico", message.Topico },
            { "Processado", message.Processado },
            { "Tentativas", message.Tentativas },
            { "CriadoEm", Timestamp.FromDateTime(message.CriadoEm) }
        };

        if (UnitOfWorkFirestore?.TransacaoAtual != null)
        {
            UnitOfWorkFirestore.TransacaoAtual.Set(docRef, dados);
        }
        else
        {
            await docRef.SetAsync(dados, cancellationToken: ct);
        }
    }

    private OutboxMessage ToModel(DocumentSnapshot doc)
    {
        var dados = doc.ToDictionary();

        Guid id = Guid.Parse(doc.Id);
        string payload = dados["Payload"].ToString() ?? string.Empty;
        string tipo = dados["Tipo"].ToString() ?? string.Empty;
        string topico = dados["Topico"].ToString() ?? string.Empty;
        int tentativas = Convert.ToInt32(dados["Tentativas"]);
        bool processado = Convert.ToBoolean(dados["Processado"]);

        DateTime criadoEm = dados["CriadoEm"] is Timestamp ts
            ? ts.ToDateTime()
            : Convert.ToDateTime(dados["CriadoEm"]);

        return new OutboxMessage(id, payload, tipo, topico, tentativas, processado, criadoEm);
    }
}