using MediatR;
using SaraBank.Domain.Interfaces;

namespace SaraBank.Application.Commands
{
    public record CadastrarUsuarioCommand(
        string Nome,
        string CPF,
        string Email,
        decimal SaldoInicial, Guid RequestId) : IRequest<string>, IIdempotentCommand;
}
