using MediatR;

namespace SaraBank.Application.Commands;

public class CriarMovimentacaoCommand : IRequest<bool>
{
    public Guid ContaId { get; set; }
    public decimal Valor { get; set; }
    public string Tipo { get; set; } // "Credito" ou "Debito"

    public CriarMovimentacaoCommand(Guid contaId, decimal valor, string tipo)
    {
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
    }
}