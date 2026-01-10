using Google.Cloud.Firestore;

namespace SaraBank.Domain.Entities;

[FirestoreData]
public class Movimentacao
{
    [FirestoreDocumentId]
    public Guid Id { get; private set; }

    [FirestoreProperty]
    public Guid ContaId { get; private set; }

    [FirestoreProperty]
    public decimal Valor { get; private set; }

    [FirestoreProperty]
    public string Tipo { get; private set; } // DEBITO / CREDITO

    [FirestoreProperty]
    public string Descricao { get; private set; }

    [FirestoreProperty]
    public DateTime Data { get; private set; }

    [FirestoreProperty]
    public Guid? SagaId { get; private set; }

    public Movimentacao() { }

    // Construtor Base para novas movimentações comuns
    public Movimentacao(Guid contaId, decimal valor, string tipo, string descricao)
        : this(Guid.NewGuid(), contaId, valor, tipo, descricao, DateTime.UtcNow, null)
    {
    }

    // Construtor Base para novas movimentações de SAGA
    public Movimentacao(Guid contaId, decimal valor, string tipo, string descricao, Guid sagaId)
        : this(Guid.NewGuid(), contaId, valor, tipo, descricao, DateTime.UtcNow, sagaId)
    {
    }

    // O Construtor "Mestre" que todos os outros chamam
    public Movimentacao(Guid id, Guid contaId, decimal valor, string tipo, string descricao, DateTime data, Guid? sagaId = null)
    {
        Id = id;
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        Data = data;
        SagaId = sagaId;
    }
}