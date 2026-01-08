using MediatR;

namespace SaraBank.Application.Events;
public class MovimentacaoRealizadaEvent : INotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContaId { get; set; }
    public decimal Valor { get; set; }
    public string Tipo { get; set; } // "Deposito", "Saque", "Transferencia"
    public DateTime OcorridoEm { get; set; } = DateTime.UtcNow;

    public MovimentacaoRealizadaEvent(string contaId, decimal valor, string tipo)
    {
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
    }
}