using MediatR;

namespace SaraBank.Application.Commands;

public class SolicitarMovimentacaoCommand : IRequest<bool>
{
    public Guid ContaId { get; set; }
    public decimal Valor { get; set; }
    public string Tipo { get; set; }

    public SolicitarMovimentacaoCommand(Guid contaId, decimal valor, string tipo)
    {
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
    }
}