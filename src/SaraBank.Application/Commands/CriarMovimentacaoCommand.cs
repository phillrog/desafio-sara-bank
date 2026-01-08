using MediatR;

namespace SaraBank.Application.Commands;

public class CriarMovimentacaoCommand : IRequest<bool>
{
    public string ContaId { get; set; }
    public decimal Valor { get; set; }
    public string Tipo { get; set; } // "Deposito" ou "Saque"

    public CriarMovimentacaoCommand(string contaId, decimal valor, string tipo)
    {
        ContaId = contaId;
        Valor = valor;
        Tipo = tipo;
    }
}