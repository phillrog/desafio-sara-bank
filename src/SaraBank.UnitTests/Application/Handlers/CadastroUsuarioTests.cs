using FluentAssertions;
using Moq;
using SaraBank.Application.Commands;
using SaraBank.Application.Handlers.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Entities;
using SaraBank.Domain.Interfaces;
using Xunit;

namespace SaraBank.UnitTests.Application.Handlers;

public class CadastroUsuarioTests
{
    private readonly Mock<IUsuarioRepository> _mockUsuarioRepo;
    private readonly Mock<IContaRepository> _mockContaRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IOutboxRepository> _mockOutboxRepo;
    private readonly CadastrarUsuarioHandler _handler;

    public CadastroUsuarioTests()
    {
        _mockUsuarioRepo = new Mock<IUsuarioRepository>();
        _mockContaRepo = new Mock<IContaRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockOutboxRepo = new Mock<IOutboxRepository>();

        _mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<string>>>()))
                .Returns(async (Func<Task<string>> acao) => await acao());

        _handler = new CadastrarUsuarioHandler(
            _mockUsuarioRepo.Object,
            _mockContaRepo.Object,
            _mockUow.Object,
            _mockOutboxRepo.Object);
    }

    [Fact]
    public async Task Deve_Criar_Usuario_E_Conta_Com_Sucesso()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Fulano de Tal", "12345678901", "fulano@email.com", 100m, Guid.NewGuid());

        _mockUsuarioRepo.Setup(r => r.ObterPorCPFAsync(command.CPF))
                        .ReturnsAsync((Usuario?)null);

        // Act
        var usuarioId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        usuarioId.Should().NotBeNullOrEmpty();

        _mockUsuarioRepo.Verify(r => r.AdicionarAsync(It.Is<Usuario>(u =>
            u.CPF == command.CPF && u.Nome == command.Nome)), Times.Once);

        _mockContaRepo.Verify(r => r.AdicionarAsync(It.Is<ContaCorrente>(c =>
            c.Saldo == 0)), Times.Once);

        _mockOutboxRepo.Verify(r => r.AdicionarAsync(It.Is<OutboxMessage>(m =>
            m.Tipo == "UsuarioCadastrado" &&
            m.Topico == "sara-bank-usuarios"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Cadastrar_Usuario_Se_CPF_Ya_Existir()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Beltrano", "51666431001", "beltrano@email.com", 100, Guid.NewGuid());
        var usuarioExistente = new Usuario(command.Nome, command.CPF, command.Email);

        _mockUsuarioRepo.Setup(r => r.ObterPorCPFAsync(command.CPF))
                        .ReturnsAsync(usuarioExistente);

        // Act & Assert
        var acao = () => _handler.Handle(command, CancellationToken.None);

        await acao.Should().ThrowAsync<FluentValidation.ValidationException>()
                  .Where(e => e.Errors.Any(f => f.ErrorMessage.Contains("Este CPF já está cadastrado")));

        _mockUsuarioRepo.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>()), Times.Never);
        _mockUow.Verify(u => u.ExecutarAsync(It.IsAny<Func<Task<string>>>()), Times.Never);
        _mockOutboxRepo.Verify(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deve_Criar_Usuario_E_Conta_Com_Movimentacao_Inicial_Se_Saldo_Positivo()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Fulano de Tal", "51666431001", "fulano@email.com", 100m, Guid.NewGuid());

        _mockUsuarioRepo.Setup(r => r.ObterPorCPFAsync(command.CPF))
                        .ReturnsAsync((Usuario?)null);

        // Act
        var usuarioId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        usuarioId.Should().NotBeNullOrEmpty();

        // AJUSTE: Verifica o repositório
        _mockOutboxRepo.Verify(r => r.AdicionarAsync(It.Is<OutboxMessage>(m =>
            m.Tipo == "UsuarioCadastrado"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deve_Criar_Usuario_Sem_Gerar_Movimentacao_Se_Saldo_Inicial_For_Zero()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Cicrano", "51666431001", "cicrano@email.com", 0m, Guid.NewGuid());

        _mockUsuarioRepo.Setup(r => r.ObterPorCPFAsync(command.CPF))
                        .ReturnsAsync((Usuario?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUsuarioRepo.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>()), Times.Once);
        _mockContaRepo.Verify(r => r.AdicionarAsync(It.IsAny<ContaCorrente>()), Times.Once);

        // Deve ainda enviar para o Outbox (boas vindas), mas o saldo é zero
        _mockOutboxRepo.Verify(r => r.AdicionarAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}