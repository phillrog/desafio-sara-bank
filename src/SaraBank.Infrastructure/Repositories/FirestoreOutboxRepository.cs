using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;

namespace SaraBank.Infrastructure.Repositories;

public class FirestoreOutboxRepository : IOutboxRepository
{
    private readonly FirestoreDb _db;

    public FirestoreOutboxRepository(FirestoreDb db) => _db = db;

    public async Task<IEnumerable<OutboxMessage>> ObterNaoProcessadosAsync(int limite, CancellationToken ct)
    {
        var collection = _db.Collection("Outbox");
        var query = collection.WhereEqualTo("Processado", false).Limit(limite);
        var snapshot = await query.GetSnapshotAsync(ct);

        var mensagens = new List<OutboxMessage>();
        foreach (var doc in snapshot.Documents)
        {
            mensagens.Add(new OutboxMessage(
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
}