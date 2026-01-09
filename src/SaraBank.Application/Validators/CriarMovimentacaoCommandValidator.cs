using FluentValidation;
using SaraBank.Application.Commands;

namespace SaraBank.Application.Validators;

public class CriarMovimentacaoCommandValidator : AbstractValidator<CriarMovimentacaoCommand>
{
    public CriarMovimentacaoCommandValidator()
    {
        RuleFor(x => x.ContaId)
            .NotEmpty().WithMessage("O ID da conta é obrigatório.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("O valor da movimentação deve ser maior que zero.");

        RuleFor(x => x.Tipo)            
            .NotEmpty().WithMessage("O tipo da movimentação é obrigatório.")
            .Must(tipo => (tipo ?? "").Equals("Deposito", StringComparison.OrdinalIgnoreCase) ||
                          (tipo ?? "").Equals("Saque", StringComparison.OrdinalIgnoreCase))
            .WithMessage("O tipo de movimentação deve ser 'Deposito' ou 'Saque'.");
    }
}