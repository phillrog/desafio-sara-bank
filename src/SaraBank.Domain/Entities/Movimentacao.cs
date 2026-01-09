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

    public Movimentacao() { } 

    public Movimentacao(Guid contaId, decimal valor, string tipo, string descricao)
    {
        Id = Guid.NewGuid();
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        Data = DateTime.UtcNow;
    }

    public Movimentacao(Guid id, Guid contaId, decimal valor, string tipo, string descricao, DateTime data)
    {
        Id = id;
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        Data = data;
    }
}