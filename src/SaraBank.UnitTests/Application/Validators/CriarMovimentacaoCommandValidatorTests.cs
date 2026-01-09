using FluentValidation.TestHelper;
using SaraBank.Application.Commands;
using SaraBank.Application.Validators;
using Xunit;

namespace SaraBank.UnitTests.Application.Validators;

public class CriarMovimentacaoCommandValidatorTests
{
    private readonly CriarMovimentacaoCommandValidator _validator;

    public CriarMovimentacaoCommandValidatorTests()
    {
        _validator = new CriarMovimentacaoCommandValidator();
    }

    [Theory]
    [InlineData("Credito")]
    [InlineData("Debito")]
    [InlineData("Credito")]
    [InlineData("Debito")]
    public void Deve_Passar_Quando_Tipo_For_Valido(string tipoValido)
    {
        // Arrange
        var command = new CriarMovimentacaoCommand(Guid.NewGuid(), 100, tipoValido);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Tipo);
    }

    [Theory]
    [InlineData("Pix")]
    [InlineData("Transferencia")]
    public void Deve_Gerar_Erro_Quando_Tipo_For_Invalido(string tipoInvalido)
    {
        // Arrange
        var command = new CriarMovimentacaoCommand(Guid.NewGuid(), 100, tipoInvalido);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tipo)
              .WithErrorMessage("O tipo de movimentação deve ser 'Debito' ou 'Credito'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Deve_Gerar_Erro_Quando_Tipo_For_Nulo(string tipoInvalido)
    {
        // Arrange
        var command = new CriarMovimentacaoCommand(Guid.NewGuid(), 100, tipoInvalido);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tipo)
              .WithErrorMessage("O tipo da movimentação é obrigatório.");
    }

    [Fact]
    public void Deve_Gerar_Erro_Quando_Valor_For_Zero_Ou_Negativo()
    {
        // Arrange
        var command = new CriarMovimentacaoCommand(Guid.NewGuid(), -10, "Credito");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Valor)
              .WithErrorMessage("O valor da movimentação deve ser maior que zero.");
    }

    [Fact]
    public void Deve_Gerar_Erro_Quando_ContaId_For_Vazio()
    {
        // Arrange
        var command = new CriarMovimentacaoCommand(Guid.Empty, 100, "Credito");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContaId);
    }
}