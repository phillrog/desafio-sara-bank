using FluentValidation;
using SaraBank.Application.Commands;

namespace SaraBank.Application.Validators;

public class SolicitarMovimentacaoValidator : AbstractValidator<SolicitarMovimentacaoCommand>
{
    public SolicitarMovimentacaoValidator()
    {
        RuleFor(x => x.ContaId)
            .NotEmpty().WithMessage("O ID da conta é obrigatório.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("O valor da movimentação deve ser superior a zero.");

        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("O tipo de movimentação deve ser informado.")
            .Must(tipo => (tipo ?? "").Equals("Debito", StringComparison.OrdinalIgnoreCase) ||
                          (tipo ?? "").Equals("Credito", StringComparison.OrdinalIgnoreCase))
            .WithMessage("O tipo de movimentação deve ser 'Debito' ou 'Credito'.");
    }
}