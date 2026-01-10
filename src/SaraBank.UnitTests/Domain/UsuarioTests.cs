using FluentAssertions;
using SaraBank.Domain.Entities;
using Xunit;

namespace SaraBank.UnitTests.Domain;

public class UsuarioTests
{
    [Fact]
    public void Construtor_DeveInicializarPropriedadesCorretamente()
    {
        // Arrange
        var nome = "Sara Oliveira";
        var cpf = "12345678901";
        var email = "sara@bank.com";

        // Act
        var usuario = new Usuario(nome, cpf, email);

        // Assert
        usuario.Id.Should().NotBeEmpty();
        usuario.Nome.Should().Be(nome);
        usuario.CPF.Should().Be(cpf);
        usuario.Email.Should().Be(email);
    }

    [Fact]
    public void Usuario_DeveGerarIdsDiferentesParaNovasInstancias()
    {
        // Act
        var usuario1 = new Usuario("User 1", "CPF1", "email1@test.com");
        var usuario2 = new Usuario("User 2", "CPF2", "email2@test.com");

        // Assert
        usuario1.Id.Should().NotBe(usuario2.Id);
    }
}