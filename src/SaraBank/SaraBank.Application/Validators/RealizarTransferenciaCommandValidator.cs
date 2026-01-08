using FluentValidation;
using SaraBank.Application.Commands;

namespace SaraBank.Application.Validators;

public class RealizarTransferenciaCommandValidator : AbstractValidator<RealizarTransferenciaCommand>
{
    public RealizarTransferenciaCommandValidator()
    {
        RuleFor(x => x.ContaOrigemId)
            .NotEmpty().WithMessage("A conta de origem é obrigatória.");

        RuleFor(x => x.ContaDestinoId)
            .NotEmpty().WithMessage("A conta de destino é obrigatória.")
            .NotEqual(x => x.ContaOrigemId).WithMessage("A conta de destino não pode ser igual à conta de origem.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("O valor da transferência deve ser maior que zero.");
    }
}