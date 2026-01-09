using FluentValidation.TestHelper;
using Moq;
using SaraBank.Application.Commands;
using SaraBank.Application.Validators;

namespace SaraBank.Tests.Application.Validators;

public class CadastrarUsuarioCommandValidatorTests
{
    private readonly CadastrarUsuarioCommandValidator _validator;

    public CadastrarUsuarioCommandValidatorTests()
    {
        _validator = new CadastrarUsuarioCommandValidator();
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_Email_For_Invalido()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Usuario Teste", "12345678901", "email-invalido", 100, It.IsAny<Guid>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("O formato do e-mail é inválido.");
    }

    [Theory]
    [InlineData("11111111111")] // Repetidos
    [InlineData("12345678901")] // Algoritmo inválido
    [InlineData("123")]         // Curto demais
    [InlineData("teste")]       // Curto demais
    public void Deve_Ter_Erro_Quando_CPF_For_Invalido(string cpfInvalido)
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Usuario Teste", cpfInvalido, "teste@sarabank.com", 100, It.IsAny<Guid>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CPF);
    }

    [Fact]
    public void Deve_Passar_Quando_Comando_For_Valido()
    {
        // Arrange - Use um CPF válido real (gerado ou conhecido)
        // Exemplo de CPF válido: 04444444405 (apenas exemplo)
        var command = new CadastrarUsuarioCommand("Lucas Silva", "51666431001", "lucas@email.com", 500, It.IsAny<Guid>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_Saldo_For_Negativo()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Usuario Teste", "70413481070", "teste@email.com", -10, It.IsAny<Guid>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SaldoInicial);
    }
}