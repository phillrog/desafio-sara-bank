using MediatR;

namespace SaraBank.Application.Commands
{
    public record RealizarTransferenciaCommand(
    string ContaOrigemId,
    string ContaDestinoId,
    decimal Valor) : IRequest<bool>;
}
