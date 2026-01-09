using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly FirestoreDb _firestoreDb;
    private const string CollectionName = "idempotencia";

    public IdempotencyRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<bool> ChaveJaExisteAsync(Guid chave)
    {
        var docRef = _firestoreDb.Collection(CollectionName).Document(chave.ToString());
        var snapshot = await docRef.GetSnapshotAsync();
        return snapshot.Exists;
    }

    public async Task SalvarChaveAsync(Guid chave, string nomeComando)
    {
        var docRef = _firestoreDb.Collection(CollectionName).Document(chave.ToString());
        var dados = new Dictionary<string, object>
        {
            { "Comando", nomeComando },
            { "DataProcessamento", Timestamp.FromDateTime(DateTime.UtcNow) }
        };

        await docRef.SetAsync(dados);
    }
}