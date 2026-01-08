using MediatR;
namespace SaraBank.Application.Commands
{
    public record CadastrarUsuarioCommand(
        string Nome,
        string CPF,
        string Email,
        decimal SaldoInicial = 0) : IRequest<string>;
}
