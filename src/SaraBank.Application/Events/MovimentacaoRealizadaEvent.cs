using MediatR;
using System.Text.Json.Serialization;

namespace SaraBank.Application.Events;

public class MovimentacaoRealizadaEvent : INotification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public decimal Valor { get; set; }

    public string Tipo { get; set; }
    
    public Guid ContaId { get; set; }

    public MovimentacaoRealizadaEvent() { }

    
    public MovimentacaoRealizadaEvent(Guid contaId, decimal valor, string tipo)
    {
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
    }
}