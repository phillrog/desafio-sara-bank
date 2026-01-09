using Google.Cloud.Firestore;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using SaraBank.Infrastructure.Persistence;
using System.Linq;

namespace SaraBank.Infrastructure.Repositories;

public class ContaRepository : IContaRepository
{
    private readonly FirestoreDb _db;
    private readonly IUnitOfWork _uow;
    private const string NomeColecao = "Contas";
    private const string NomeColecaoIdempotencia = "Idempotencia";
    private FirestoreUnitOfWork? UnitOfWorkFirestore => _uow as FirestoreUnitOfWork;

    public ContaRepository(FirestoreDb db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }


    public async Task AdicionarAsync(ContaCorrente conta)
    {
        var docRef = _db.Collection(NomeColecao).Document(conta.Id.ToString());

        if (UnitOfWorkFirestore?.TransacaoAtual != null)
        {
            UnitOfWorkFirestore.TransacaoAtual.Set(docRef, conta);
            await Task.CompletedTask;
        }
        else
        {
            await docRef.SetAsync(conta);
        }
    }

    public async Task<ContaCorrente> ObterPorIdAsync(Guid id)
    {
        var docRef = _db.Collection(NomeColecao).Document(id.ToString());

        DocumentSnapshot snapshot = UnitOfWorkFirestore?.TransacaoAtual != null
            ? await UnitOfWorkFirestore.TransacaoAtual.GetSnapshotAsync(docRef)
            : await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
            throw new Exception("Conta corrente não encontrada no SARA Bank.");

        var dados = snapshot.ToDictionary();

        return new ContaCorrente(Guid.Parse(snapshot.Id),
            dados.ContainsKey("UsuarioId")
                    ? Guid.Parse(dados["UsuarioId"].ToString())
                    : Guid.Empty,
            dados.ContainsKey("Saldo")
                ? Convert.ToDecimal(dados["Saldo"])
                : 0m
        );
    }

    public async Task<ContaCorrente?> ObterPorUsuarioIdAsync(Guid usuarioId)
    {
        var query = _db.Collection(NomeColecao).WhereEqualTo("UsuarioId", usuarioId.ToString()).Limit(1);

        var snapshot = (UnitOfWorkFirestore?.TransacaoAtual != null)
            ? await UnitOfWorkFirestore.TransacaoAtual.GetSnapshotAsync(query)
            : await query.GetSnapshotAsync();

        if (snapshot.Documents.Count == 0) return null;

        var dados = snapshot.Documents[0].ToDictionary();

        return new ContaCorrente(Guid.Parse(snapshot.Documents[0].Id),
            dados.ContainsKey("UsuarioId")
                    ? Guid.Parse(dados["UsuarioId"].ToString())
                    : Guid.Empty,
            dados.ContainsKey("Saldo")
                ? Convert.ToDecimal(dados["Saldo"])
                : 0m
        );
    }

    public async Task AtualizarAsync(ContaCorrente conta)
    {
        var docRef = _db.Collection(NomeColecao).Document(conta.Id.ToString());

        if (UnitOfWorkFirestore?.TransacaoAtual != null)
        {
            UnitOfWorkFirestore.TransacaoAtual.Set(docRef, conta);
        }
        else
        {
            await docRef.SetAsync(conta);
        }
    }

    public async Task<bool> VerificarIdempotenciaAsync(Guid chave)
    {
        var docRef = _db.Collection(NomeColecaoIdempotencia).Document(chave.ToString());

        var snapshot = UnitOfWorkFirestore?.TransacaoAtual != null
            ? await UnitOfWorkFirestore.TransacaoAtual.GetSnapshotAsync(docRef)
            : await docRef.GetSnapshotAsync();

        return snapshot.Exists;
    }

    public async Task RegistrarChaveIdempotenciaAsync(Guid chave)
    {
        var docRef = _db.Collection(NomeColecaoIdempotencia).Document(chave.ToString());
        var dados = new
        {
            DataProcessamento = Timestamp.GetCurrentTimestamp(),
            Descricao = "Operação processada com sucesso"
        };

        if (UnitOfWorkFirestore?.TransacaoAtual != null)
        {
            UnitOfWorkFirestore.TransacaoAtual.Create(docRef, dados);
        }
        else
        {
            await docRef.CreateAsync(dados);
        }
    }
}