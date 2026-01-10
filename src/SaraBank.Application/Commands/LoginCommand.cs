using MediatR;
namespace SaraBank.Application.Commands;
public record LoginCommand(string Email, string Senha) : IRequest<string>;

