using MediatR;

namespace SaraBank.Application.Commands
{
    public record RealizarTransferenciaCommand(
    Guid ContaOrigemId,
    Guid ContaDestinoId,
    decimal Valor) : IRequest<bool>;
}
