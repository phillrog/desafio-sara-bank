using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Moq;
using SaraBank.Application.Commands;
using SaraBank.Application.Validators;
using SaraBank.Domain.Entities;
using System.Reflection.Metadata;

namespace SaraBank.UnitTests.Application.Validators;

public class RealizarTransferenciaCommandValidatorTests
{
    private readonly RealizarTransferenciaCommandValidator _validator;

    public RealizarTransferenciaCommandValidatorTests()
    {
        _validator = new RealizarTransferenciaCommandValidator();
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_Valor_For_Zero_Ou_Negativo()
    {
        // Arrange
        var command = new RealizarTransferenciaCommand(It.IsAny<Guid>(), It.IsAny<Guid>(), 0m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Valor)
              .WithErrorMessage("O valor da transferência deve ser maior que zero.");
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_Contas_Origem_E_Destino_Forem_Iguais()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var command = new RealizarTransferenciaCommand(contaId, contaId, 150.00m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContaDestinoId)
              .WithErrorMessage("A conta de destino não pode ser igual à conta de origem.");
    }

    [Fact]
    public void Deve_Ter_Erro_Quando_Campos_Obrigatorios_Estiverem_Vazios()
    {
        // Arrange
        var contaInvalida = Guid.Empty;
        var command = new RealizarTransferenciaCommand(contaInvalida, contaInvalida, 10.00m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContaOrigemId);
        result.ShouldHaveValidationErrorFor(x => x.ContaDestinoId);
    }

    [Fact]
    public void Nao_Deve_Ter_Erro_Quando_Comando_For_Valido()
    {
        // Arrange
        var contaOrigem = Guid.NewGuid();
        var contaDestino = Guid.NewGuid();
        var command = new RealizarTransferenciaCommand(contaOrigem, contaDestino, 250.75m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}