using MediatR;

namespace SaraBank.Application.Commands;

public record SacarCommand(
    string ContaId,
    decimal Valor) : IRequest<bool>;