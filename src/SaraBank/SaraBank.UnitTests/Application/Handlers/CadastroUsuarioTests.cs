using Moq;
using Xunit;
using FluentAssertions;
using SaraBank.Domain.Entities;
using SaraBank.Application.Handlers;
using SaraBank.Application.Commands;
using SaraBank.Application.Interfaces;
using SaraBank.Domain.Interfaces;

namespace SaraBank.UnitTests.Application.Handlers;

public class CadastroUsuarioTests
{
    private readonly Mock<IUsuarioRepository> _mockUsuarioRepo;
    private readonly Mock<IContaRepository> _mockContaRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly CadastrarUsuarioHandler _handler;

    public CadastroUsuarioTests()
    {
        _mockUsuarioRepo = new Mock<IUsuarioRepository>();
        _mockContaRepo = new Mock<IContaRepository>();
        _mockUow = new Mock<IUnitOfWork>();

        _mockUow.Setup(u => u.ExecutarAsync(It.IsAny<Func<Task<string>>>()))
                .Returns(async (Func<Task<string>> acao) => await acao());

        _handler = new CadastrarUsuarioHandler(
            _mockUsuarioRepo.Object,
            _mockContaRepo.Object,
            _mockUow.Object);
    }

    [Fact]
    public async Task Deve_Criar_Usuario_E_Conta_Com_Sucesso()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Fulano de Tal", "12345678901", "fulano@email.com", 100m);

        _mockUsuarioRepo.Setup(r => r.ObterPorCPFAsync(command.CPF))
                        .ReturnsAsync((Usuario?)null);

        // Act
        var usuarioId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        usuarioId.Should().NotBeNullOrEmpty();

        // Verifica se o usuário foi adicionado
        _mockUsuarioRepo.Verify(r => r.AdicionarAsync(It.Is<Usuario>(u =>
            u.CPF == command.CPF && u.Nome == command.Nome)), Times.Once);

        // Verifica se a conta foi aberta para esse usuário com o saldo inicial correto
        _mockContaRepo.Verify(r => r.AdicionarAsync(It.Is<ContaCorrente>(c =>
            c.Saldo == command.SaldoInicial)), Times.Once);

        // Verifica se o evento de boas-vindas foi para o Outbox
        _mockUow.Verify(u => u.AdicionarAoOutboxAsync(It.IsAny<string>(), "UsuarioCadastrado"), Times.Once);
    }

    [Fact]
    public async Task Nao_Deve_Cadastrar_Usuario_Se_CPF_Ja_Existir()
    {
        // Arrange
        var command = new CadastrarUsuarioCommand("Beltrano", "12345678901", "beltrano@email.com");
        var usuarioExistente = new Usuario(command.Nome, command.CPF, command.Email);

        _mockUsuarioRepo.Setup(r => r.ObterPorCPFAsync(command.CPF))
                        .ReturnsAsync(usuarioExistente);

        // Act & Assert
        var acao = () => _handler.Handle(command, CancellationToken.None);

        await acao.Should().ThrowAsync<InvalidOperationException>()
                  .WithMessage("Já existe um usuário cadastrado com este CPF no SARA Bank.");

        // Garante que nem tentou abrir conta ou salvar usuário novo
        _mockUsuarioRepo.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>()), Times.Never);
        _mockUow.Verify(u => u.ExecutarAsync(It.IsAny<Func<Task<string>>>()), Times.Never);
    }
}