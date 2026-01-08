using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;

namespace SaraBank.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly FirestoreDb _db;
    private readonly IUnitOfWork _uow;

    public UsuarioRepository(FirestoreDb db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task AdicionarAsync(Usuario usuario)
    {
        var docRef = _db.Collection("Usuarios").Document(usuario.Id.ToString());

        if (_uow is FirestoreUnitOfWork fUow && fUow.TransacaoAtual != null)
        {
            fUow.TransacaoAtual.Set(docRef, usuario);
            await Task.CompletedTask;
        }
        else
        {
            await docRef.SetAsync(usuario);
        }
    }

    public async Task<Usuario?> ObterPorIdAsync(string id)
    {
        var docRef = _db.Collection("Usuarios").Document(id);

        var snapshot = (_uow is FirestoreUnitOfWork fUow && fUow.TransacaoAtual != null)
            ? await fUow.TransacaoAtual.GetSnapshotAsync(docRef)
            : await docRef.GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<Usuario>() : null;
    }

    public async Task<Usuario?> ObterPorCPFAsync(string cpf)
    {
        var query = _db.Collection("Usuarios").WhereEqualTo("CPF", cpf).Limit(1);

        var snapshot = (_uow is FirestoreUnitOfWork fUow && fUow.TransacaoAtual != null)
            ? await fUow.TransacaoAtual.GetSnapshotAsync(query)
            : await query.GetSnapshotAsync();

        return snapshot.Documents.Count > 0 ? snapshot.Documents[0].ConvertTo<Usuario>() : null;
    }
}