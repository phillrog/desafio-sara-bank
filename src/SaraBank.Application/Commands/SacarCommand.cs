using MediatR;

namespace SaraBank.Application.Commands;

public record SacarCommand(
    Guid ContaId,
    decimal Valor) : IRequest<bool>;