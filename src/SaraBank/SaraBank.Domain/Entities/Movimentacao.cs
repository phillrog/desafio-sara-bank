using Google.Cloud.Firestore;

namespace SaraBank.Domain.Entities;

[FirestoreData]
public class Movimentacao
{
    [FirestoreDocumentId]
    public string Id { get; set; }

    [FirestoreProperty]
    public string ContaId { get; set; }

    [FirestoreProperty]
    public decimal Valor { get; set; }

    [FirestoreProperty]
    public string Tipo { get; set; } // DEBITO / CREDITO

    [FirestoreProperty]
    public string Descricao { get; set; }

    [FirestoreProperty]
    public DateTime Data { get; set; }

    public Movimentacao() { } 

    public Movimentacao(string contaId, decimal valor, string tipo, string descricao)
    {
        Id = Guid.NewGuid().ToString();
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        Data = DateTime.UtcNow;
    }
}