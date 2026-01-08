using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;

namespace SaraBank.Infrastructure.Repositories;

public class FirestoreOutboxRepository : IOutboxRepository
{
    private readonly FirestoreDb _db;

    public FirestoreOutboxRepository(FirestoreDb db) => _db = db;

    public async Task<IEnumerable<OutboxMessageDTO>> ObterNaoProcessadosAsync(int limite, CancellationToken ct)
    {
        var collection = _db.Collection("Outbox");
        var query = collection.WhereEqualTo("Processado", false).Limit(limite);
        var snapshot = await query.GetSnapshotAsync(ct);

        var mensagens = new List<OutboxMessageDTO>();
        foreach (var doc in snapshot.Documents)
        {
            mensagens.Add(new OutboxMessageDTO(
                doc.Id,
                doc.GetValue<string>("Payload"),
                doc.GetValue<string>("Tipo")
            ));
        }
        return mensagens;
    }

    public async Task MarcarComoProcessadoAsync(string id, CancellationToken ct)
    {
        var docRef = _db.Collection("Outbox").Document(id);
        await docRef.UpdateAsync("Processado", true);
    }

    public async Task AdicionarAsync(OutboxMessageDTO message, CancellationToken ct)
    {
        var docRef = _db.Collection("Outbox").Document(message.Id);

        var dados = new Dictionary<string, object>
        {
            { "Id", message.Id },
            { "Payload", message.Payload },
            { "Tipo", message.Tipo },
            { "Processado", false },
            { "CriadoEm", DateTime.UtcNow }
        };

        await docRef.SetAsync(dados, cancellationToken: ct);
    }
}