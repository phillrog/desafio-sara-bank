using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using SaraBank.Application.Behaviors;
using Xunit;

namespace SaraBank.UnitTests.Application.Behaviors;

public class ValidationBehaviorTests
{
    // Criamos um comando e resposta genéricos apenas para o teste
    public record TestRequest() : IRequest<bool>;

    [Fact]
    public async Task Deve_Lancar_ValidationException_Quando_Existirem_Erros_De_Validacao()
    {
        // Arrange
        var request = new TestRequest();
        var failure = new ValidationFailure("Propriedade", "Mensagem de erro");

        // Mock do Validador: simula que ele encontrou um erro
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { failure }));

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, bool>(validators);

        // O 'next' representa o Handler (que não deve ser chamado se houver erro)
        var nextCalled = false;
        RequestHandlerDelegate<bool> next = (_) =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act & Assert
        // Verifica se o Behavior lança a exceção esperada
        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(request, next, CancellationToken.None));

        // Verifica se o Handler (next) JAMAIS foi chamado
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Deve_Chamar_Proximo_Passo_Quando_Nao_Existirem_Erros_De_Validacao()
    {
        // Arrange
        var request = new TestRequest();
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // Sem erros

        var validators = new List<IValidator<TestRequest>> { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, bool>(validators);

        var nextCalled = false;
        RequestHandlerDelegate<bool> next = (_) =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        // Verifica se, como tudo estava certo, ele seguiu para o Handler
        Assert.True(nextCalled);
    }
}