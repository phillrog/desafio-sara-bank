using MediatR;
using System.Text.Json.Serialization;

namespace SaraBank.Application.Events;

public class MovimentacaoRealizadaEvent : INotification
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; }
    
    [JsonPropertyName("contaId")]
    public Guid ContaId { get; set; }

    public MovimentacaoRealizadaEvent() { }

    public MovimentacaoRealizadaEvent(Guid contaId, decimal valor, string tipo)
    {
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
    }
}