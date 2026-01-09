using FluentValidation.TestHelper;
using SaraBank.Application.Commands;
using SaraBank.Application.Validators;
using Xunit;

namespace SaraBank.UnitTests.Application.Validators;

public class SolicitarMovimentacaoValidatorTests
{
    private readonly SolicitarMovimentacaoValidator _validator;

    public SolicitarMovimentacaoValidatorTests()
    {
        _validator = new SolicitarMovimentacaoValidator();
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_ContaId_For_Vazio()
    {
        // Arrange
        var command = new SolicitarMovimentacaoCommand(Guid.Empty, 100, "Credito");

        // Act & Assert
        var resultado = _validator.TestValidate(command);
        resultado.ShouldHaveValidationErrorFor(x => x.ContaId)
                 .WithErrorMessage("O ID da conta é obrigatório.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50.50)]
    public void Deve_Ter_Erro_Quando_Valor_For_Menor_Ou_Igual_A_Zero(decimal valorInvalido)
    {
        // Arrange
        var command = new SolicitarMovimentacaoCommand(Guid.NewGuid(), valorInvalido, "Credito");

        // Act & Assert
        var resultado = _validator.TestValidate(command);
        resultado.ShouldHaveValidationErrorFor(x => x.Valor)
                 .WithErrorMessage("O valor da movimentação deve ser superior a zero.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Transferencia")]
    [InlineData("Saque")] // deve falhar 'Saque'
    public void Deve_Ter_Erro_Quando_Tipo_For_Invalido(string tipoInvalido)
    {
        // Arrange
        var command = new SolicitarMovimentacaoCommand(Guid.NewGuid(), 100, tipoInvalido);

        // Act & Assert
        var resultado = _validator.TestValidate(command);
        resultado.ShouldHaveValidationErrorFor(x => x.Tipo);
    }

    [Theory]
    [InlineData("Credito")]
    [InlineData("Debito")]
    [InlineData("CREDITO")]
    [InlineData("debito")]
    public void Deve_Passar_Quando_Dados_Forem_Validos(string tipoValido)
    {
        // Arrange
        var command = new SolicitarMovimentacaoCommand(Guid.NewGuid(), 150.75m, tipoValido);

        // Act & Assert
        var resultado = _validator.TestValidate(command);
        resultado.ShouldNotHaveAnyValidationErrors();
    }
}