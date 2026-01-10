using FluentValidation.TestHelper;
using Moq;
using SaraBank.Application.Commands;
using SaraBank.Application.Validators;

namespace SaraBank.UnitTests.Application.Validators;

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
        var command = new CadastrarUsuarioCommand(
            "Usuario Teste",
            "12345678901",
            "email-invalido",
            "Senha123",
            "Senha123",
            100,
            Guid.NewGuid());

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
    [InlineData("teste")]       // Não numérico
    public void Deve_Ter_Erro_Quando_CPF_For_Invalido(string cpfInvalido)
    {
        // Arrange
        var command = new CadastrarUsuarioCommand(
            "Usuario Teste",
            cpfInvalido,
            "teste@sarabank.com",
            "Senha123",
            "Senha123",
            100,
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CPF);
    }

    [Fact]
    public void Deve_Passar_Quando_Comando_For_Valido()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand(
            "Lucas Silva",
            "51666431001",
            "lucas@email.com",
            "Senha123",
            "Senha123",
            500,
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_Saldo_For_Negativo()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand(
            "Usuario Teste",
            "70413481070",
            "teste@email.com",
            "Senha123",
            "Senha123",
            -10,
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SaldoInicial);
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_Senhas_Forem_Diferentes()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand(
            "Usuario Teste",
            "70413481070",
            "teste@email.com",
            "Senha123",
            "SenhaDiferente99",
            100,
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmacaoSenha)
              .WithErrorMessage("As senhas não conferem.");
    }

    [Theory]
    [InlineData("123")]          // Curta demais (< 6)
    [InlineData("senhatodaemminuscula")] // Sem maiúscula
    [InlineData("SENHATODAEMMAIUSCULA")] // Sem número
    public void Deve_Ter_Erro_Quando_Senha_For_Fraca(string senhaFraca)
    {
        // Arrange
        var command = new CadastrarUsuarioCommand(
            "Usuario Teste",
            "70413481070",
            "teste@email.com",
            senhaFraca,
            senhaFraca,
            100,
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Senha);
    }
}