using Moq;
using Xunit;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using SaraBank.Domain.Entities;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;
using SaraBank.Application.Handlers.Commands;

namespace SaraBank.UnitTests.Application.Handlers;

public class RealizarTransferenciaTests
{
    private readonly Mock<IContaRepository> _mockContaRepo;
    private readonly Mock<IMovimentacaoRepository> _mockMovimentacaoRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IValidator<RealizarTransferenciaCommand>> _mockValidator;
    private readonly RealizarTransferenciaHandler _handler;

    public RealizarTransferenciaTests()
    {
        _mockContaRepo = new Mock<IContaRepository>();
        _mockMovimentacaoRepo = new Mock<IMovimentacaoRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockValidator = new Mock<IValidator<RealizarTransferenciaCommand>>();

        _mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns(async (Func<Task<bool>> acao) => await acao());

        _handler = new RealizarTransferenciaHandler(
            _mockContaRepo.Object,
            _mockMovimentacaoRepo.Object,
            _mockUow.Object,
            _mockValidator.Object);
    }

    [Fact]
    public async Task Deve_Transferir_Com_Sucesso_Quando_Contas_Existem_E_Ha_Saldo()
    {
        // Arrange
        var contaOrigem = new ContaCorrente(Guid.NewGuid(), 500m);
        var contaDestino = new ContaCorrente(Guid.NewGuid(), 100m);

        var command = new RealizarTransferenciaCommand(contaOrigem.Id, contaDestino.Id, 200m);

        _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new ValidationResult());

        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaOrigem.Id)).ReturnsAsync(contaOrigem);
        _mockContaRepo.Setup(r => r.ObterPorIdAsync(contaDestino.Id)).ReturnsAsync(contaDestino);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().BeTrue();
        contaOrigem.Saldo.Should().Be(300m);
        contaDestino.Saldo.Should().Be(300m);

        // Verifica persistência
        _mockContaRepo.Verify(r => r.AtualizarAsync(It.IsAny<ContaCorrente>()), Times.Exactly(2));
        _mockMovimentacaoRepo.Verify(r => r.AdicionarAsync(It.IsAny<Movimentacao>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Deve_Falhar_Quando_Validacao_Do_FluentValidation_Retornar_Erro()
    {
        // Arrange
        var command = new RealizarTransferenciaCommand("origem", "destino", -50m);
        var failure = new ValidationFailure("Valor", "O valor não pode ser negativo");

        _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new ValidationResult(new[] { failure }));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));

        // Verifica se a transação sequer foi aberta
        _mockUow.Verify(u => u.ExecutarAsync(It.IsAny<Func<Task<bool>>>()), Times.Never);
    }
}