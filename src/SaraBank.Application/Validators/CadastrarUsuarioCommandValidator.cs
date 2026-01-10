using FluentValidation;
using SaraBank.Application.Commands;

namespace SaraBank.Application.Validators;

public class CadastrarUsuarioCommandValidator : AbstractValidator<CadastrarUsuarioCommand>
{
    public CadastrarUsuarioCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(3).WithMessage("O nome deve ter pelo menos 3 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O formato do e-mail é inválido.");

        RuleFor(x => x.CPF)
            .NotEmpty().WithMessage("O CPF é obrigatório.")
            .Matches(@"^\d{11}$").WithMessage("O CPF deve conter exatamente 11 números.")
            .Must(ValidarCpf).WithMessage("O CPF fornecido não é válido.");

        RuleFor(x => x.SaldoInicial)
            .GreaterThanOrEqualTo(0).WithMessage("O saldo inicial não pode ser negativo.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve ter pelo menos 6 caracteres.")
            .Matches(@"[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
            .Matches(@"[0-9]").WithMessage("A senha deve conter pelo menos um número.");

        RuleFor(x => x.ConfirmacaoSenha)
            .NotEmpty().WithMessage("A confirmação de senha é obrigatória.")
            .Equal(x => x.Senha).WithMessage("As senhas não conferem.");

    }

    // Método auxiliar simples para validação lógica de CPF (opcional)
    private bool ValidarCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;
        // Remove caracteres não numéricos (pontos, traços, etc.)
        cpf = cpf.Replace(".", "").Replace("-", "").Trim();

        // Verifica se o CPF possui 11 dígitos numéricos
        if (cpf.Length != 11)
            return false;

        // Verifica CPFs com dígitos repetidos, que são inválidos, embora passem no cálculo
        if (cpf == "00000000000" ||
            cpf == "11111111111" ||
            cpf == "22222222222" ||
            cpf == "33333333333" ||
            cpf == "44444444444" ||
            cpf == "55555555555" ||
            cpf == "66666666666" ||
            cpf == "77777777777" ||
            cpf == "88888888888" ||
            cpf == "99999999999")
            return false;

        // Implementação do algoritmo de validação (cálculo dos dígitos verificadores)
        int[] multiplicadores1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicadores2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf = cpf.Substring(0, 9);
        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicadores1[i];

        int resto = soma % 11;
        if (resto < 2)
            resto = 0;
        else
            resto = 11 - resto;

        string digito1 = resto.ToString();
        tempCpf = tempCpf + digito1;
        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicadores2[i];

        resto = soma % 11;
        if (resto < 2)
            resto = 0;
        else
            resto = 11 - resto;

        string digito2 = resto.ToString();

        // Compara os dígitos verificadores calculados com os dígitos reais do CPF
        return cpf.EndsWith(digito1 + digito2);
    }
}